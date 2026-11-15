// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Data.Sqlite;

using Papercut.Core.Domain.Message;

namespace Papercut.Message.Index;

/// <summary>
/// SQLite index over the message folder: full-text search (trigram, so any 3+ character
/// piece of a word or address matches), list-row summaries, and read status.
///
/// The .eml files stay the source of truth. The index only ever catches up to them: new
/// and edited files are (re)indexed, rows for missing files are dropped, and deleting the
/// database just rebuilds it. If it cannot be opened at all, <see cref="IsAvailable"/> is
/// false and callers fall back to reading the files.
/// </summary>
public sealed class MessageIndex : IMessageIndex
{
    public const string FileName = "papercut-index.db";

    const int SchemaVersion = 1;

    const int BatchSize = 50;

    static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    readonly string _connectionString = "";

    readonly ILogger _logger;

    readonly ConcurrentDictionary<string, Row> _rows = new(PathComparer);

    /// <summary>Opened before they were indexed; indexed as read.</summary>
    readonly ConcurrentDictionary<string, byte> _readBeforeIndexed = new(PathComparer);

    readonly SemaphoreSlim _writeLock = new(1, 1);

    volatile bool _built;

    public MessageIndex(string databaseFile, ILogger logger)
    {
        _logger = logger;

        try
        {
            _connectionString = Open(databaseFile);
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Message index unavailable at {IndexFile}: search covers sender, recipient and subject only", databaseFile);
        }
    }

    public bool IsAvailable { get; }

    public bool IsBuilt => IsAvailable && _built;

    /// <summary>
    /// Where the index goes for a message folder: beside the mail, so it moves with it (and
    /// lives in the Docker volume). SQLite locking is unreliable on network shares, so a
    /// network folder keeps its index in the local data folder instead.
    /// </summary>
    public static string LocationFor(string messageFolder, string localDataFolder)
    {
        var folder = IsNetworkPath(messageFolder) ? localDataFolder : messageFolder;

        return Path.Combine(folder, FileName);
    }

    public async Task SyncAsync(IReadOnlyCollection<MessageEntry> entries, CancellationToken token = default)
    {
        if (!IsAvailable) return;

        await _writeLock.WaitAsync(token);

        try
        {
            var current = new HashSet<string>(entries.Select(e => e.File), PathComparer);
            var gone = _rows.Keys.Where(file => !current.Contains(file)).ToList();

            if (gone.Count > 0) Remove(gone);

            await IndexAsync(Outdated(entries), token);

            if (!_built) MarkBuilt();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyDictionary<string, MessageSummary>> GetSummariesAsync(
        IReadOnlyCollection<MessageEntry> entries,
        CancellationToken token = default)
    {
        if (!IsAvailable) return new Dictionary<string, MessageSummary>();

        var outdated = Outdated(entries);

        // a page's worth is quick; but never queue a page behind a full build
        if (outdated.Count > 0 && await _writeLock.WaitAsync(0, token))
        {
            try
            {
                await IndexAsync(Outdated(entries), token);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        var summaries = new Dictionary<string, MessageSummary>(PathComparer);

        foreach (var entry in entries)
        {
            if (_rows.TryGetValue(entry.File, out var row)) summaries[entry.File] = row.Summary;
        }

        return summaries;
    }

    public async Task<IReadOnlySet<string>> SearchAsync(string search, CancellationToken token = default)
    {
        var terms = MessageSearch.Terms(search);
        var files = new HashSet<string>(PathComparer);

        if (!IsAvailable || terms.Length == 0) return files;

        await using var connection = Connect();
        await using var command = connection.CreateCommand();

        var sql = new List<string> { "SELECT file FROM messages WHERE 1 = 1" };

        // trigrams need 3 characters. Beside a longer word, a short one ("hi", "to") would only
        // narrow the search to messages with it in a header, so it is left out; on their own,
        // short words are matched against sender, recipient and subject
        var indexed = terms.Where(t => t.Length >= 3).ToList();
        if (indexed.Count > 0)
        {
            sql.Add("AND id IN (SELECT rowid FROM search WHERE search MATCH $match)");
            command.Parameters.AddWithValue("$match", string.Join(" AND ", indexed.Select(t => $"\"{t.Replace("\"", "\"\"")}\"")));
        }

        var shortTerms = indexed.Count > 0 ? [] : terms.ToList();
        for (var i = 0; i < shortTerms.Count; i++)
        {
            sql.Add($"AND (subject LIKE $s{i} ESCAPE '\\' OR addresses LIKE $s{i} ESCAPE '\\')");
            command.Parameters.AddWithValue($"$s{i}", $"%{EscapeLike(shortTerms[i])}%");
        }

        command.CommandText = string.Join(' ', sql);

        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            files.Add(reader.GetString(0));
        }

        return files;
    }

    public bool IsRead(string file) =>
        _rows.TryGetValue(file, out var row)
            ? row.Summary.IsRead
            : !_built || _readBeforeIndexed.ContainsKey(file);

    public void MarkRead(string file)
    {
        if (!IsAvailable) return;

        if (!_rows.TryGetValue(file, out var row))
        {
            _readBeforeIndexed.TryAdd(file, 0);
            return;
        }

        if (row.Summary.IsRead) return;

        _rows[file] = row with { Summary = row.Summary with { IsRead = true } };

        Write("UPDATE messages SET is_read = 1 WHERE id = $id", ("$id", row.Id));
    }

    public void MarkAllRead()
    {
        if (!IsAvailable) return;

        foreach (var (file, row) in _rows)
        {
            if (!row.Summary.IsRead) _rows[file] = row with { Summary = row.Summary with { IsRead = true } };
        }

        Write("UPDATE messages SET is_read = 1 WHERE is_read = 0");
    }

    #region Indexing

    sealed record Row(long Id, long Size, long Modified, MessageSummary Summary);

    sealed record Pending(MessageEntry Entry, long Size, long Modified);

    /// <summary>New files, and files changed on disk since they were indexed.</summary>
    List<Pending> Outdated(IEnumerable<MessageEntry> entries)
    {
        var outdated = new List<Pending>();

        foreach (var entry in entries)
        {
            long size, modified;

            try
            {
                var info = new FileInfo(entry.File);
                if (!info.Exists) continue;

                size = info.Length;
                modified = info.LastWriteTimeUtc.Ticks;
            }
            catch (IOException)
            {
                continue;
            }

            if (_rows.TryGetValue(entry.File, out var row) && row.Size == size && row.Modified == modified) continue;

            outdated.Add(new Pending(entry, size, modified));
        }

        return outdated;
    }

    async Task IndexAsync(List<Pending> pending, CancellationToken token)
    {
        if (pending.Count == 0) return;

        await using var connection = Connect();

        foreach (var batch in pending.Chunk(BatchSize))
        {
            token.ThrowIfCancellationRequested();

            // parse outside the transaction, so a slow batch never holds the database
            var documents = batch
                .Select(p => (p, Document: MessageIndexDocument.Read(p.Entry.File)))
                .Where(x => x.Document != null)
                .ToList();

            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);

            foreach (var (p, document) in documents)
            {
                Upsert(connection, transaction, p, document!);
            }

            await transaction.CommitAsync(token);
        }
    }

    void Upsert(SqliteConnection connection, SqliteTransaction transaction, Pending pending, MessageIndexDocument document)
    {
        var file = pending.Entry.File;
        var existing = _rows.TryGetValue(file, out var row) ? row : null;

        // a fresh index starts with the inbox read; after that, new mail is unread until opened
        var isRead = existing?.Summary.IsRead ?? (!_built || _readBeforeIndexed.ContainsKey(file));

        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        if (existing != null)
        {
            command.CommandText = "DELETE FROM search WHERE rowid = $id";
            command.Parameters.AddWithValue("$id", existing.Id);
            command.ExecuteNonQuery();
            command.Parameters.Clear();
        }

        command.CommandText = """
            INSERT INTO messages (file, size, modified, subject, addresses, sender, priority, attachments, is_read)
            VALUES ($file, $size, $modified, $subject, $addresses, $sender, $priority, $attachments, $read)
            ON CONFLICT (file) DO UPDATE SET
                size = excluded.size, modified = excluded.modified, subject = excluded.subject,
                addresses = excluded.addresses, sender = excluded.sender, priority = excluded.priority,
                attachments = excluded.attachments
            RETURNING id, is_read
            """;
        command.Parameters.AddWithValue("$file", file);
        command.Parameters.AddWithValue("$size", pending.Size);
        command.Parameters.AddWithValue("$modified", pending.Modified);
        command.Parameters.AddWithValue("$subject", (object?)document.Subject ?? DBNull.Value);
        command.Parameters.AddWithValue("$addresses", document.Addresses);
        command.Parameters.AddWithValue("$sender", JsonSerializer.Serialize(document.From));
        command.Parameters.AddWithValue("$priority", document.Priority);
        command.Parameters.AddWithValue("$attachments", document.AttachmentCount);
        command.Parameters.AddWithValue("$read", isRead ? 1 : 0);

        long id;
        using (var reader = command.ExecuteReader())
        {
            reader.Read();
            id = reader.GetInt64(0);
            isRead = reader.GetInt64(1) != 0;
        }

        command.Parameters.Clear();
        command.CommandText = "INSERT INTO search (rowid, subject, addresses, body, attachments) VALUES ($id, $subject, $addresses, $body, $names)";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$subject", document.Subject ?? "");
        command.Parameters.AddWithValue("$addresses", document.Addresses);
        command.Parameters.AddWithValue("$body", document.Body);
        command.Parameters.AddWithValue("$names", document.AttachmentNames);
        command.ExecuteNonQuery();

        _rows[file] = new Row(id, pending.Size, pending.Modified,
            new MessageSummary(document.Subject, document.From, document.Priority, document.AttachmentCount, isRead));
        _readBeforeIndexed.TryRemove(file, out _);
    }

    void Remove(List<string> files)
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        foreach (var file in files)
        {
            if (!_rows.TryRemove(file, out var row)) continue;

            command.Parameters.Clear();
            command.Parameters.AddWithValue("$id", row.Id);
            command.CommandText = "DELETE FROM search WHERE rowid = $id";
            command.ExecuteNonQuery();
            command.CommandText = "DELETE FROM messages WHERE id = $id";
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    void MarkBuilt()
    {
        using var connection = Connect();
        Execute(connection, "INSERT OR REPLACE INTO meta (key, value) VALUES ('built', '1')");
        _built = true;
    }

    /// <summary>
    /// Small writes outside the build lock: SQLite queues them behind any running batch,
    /// which is never more than <see cref="BatchSize"/> messages.
    /// </summary>
    void Write(string sql, params (string Name, object Value)[] parameters)
    {
        try
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to save read status to the message index");
        }
    }

    #endregion

    #region Database

    string Open(string databaseFile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databaseFile))!);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30
        }.ToString();

        try
        {
            Initialize(connectionString);
        }
        catch (SqliteException ex)
        {
            // corrupt or not a database: it only ever holds what the files can rebuild
            _logger.Warning(ex, "Rebuilding unreadable message index {IndexFile}", databaseFile);

            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                File.Delete(databaseFile + suffix);
            }

            Initialize(connectionString);
        }

        return connectionString;
    }

    void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Execute(connection, "PRAGMA journal_mode = WAL");
        Execute(connection, "CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)");

        var schema = Scalar(connection, "SELECT value FROM meta WHERE key = 'schema'");
        if (schema != null && schema != SchemaVersion.ToString())
        {
            Execute(connection, "DROP TABLE IF EXISTS search");
            Execute(connection, "DROP TABLE IF EXISTS messages");
            Execute(connection, "DELETE FROM meta");
        }

        Execute(connection, """
            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY,
                file TEXT NOT NULL UNIQUE,
                size INTEGER NOT NULL,
                modified INTEGER NOT NULL,
                subject TEXT,
                addresses TEXT NOT NULL DEFAULT '',
                sender TEXT NOT NULL DEFAULT '[]',
                priority TEXT NOT NULL DEFAULT 'Normal',
                attachments INTEGER NOT NULL DEFAULT 0,
                is_read INTEGER NOT NULL DEFAULT 0)
            """);

        // contentless: the index keeps no second copy of every body
        Execute(connection, """
            CREATE VIRTUAL TABLE IF NOT EXISTS search USING fts5(
                subject, addresses, body, attachments,
                content = '', contentless_delete = 1, tokenize = 'trigram')
            """);

        Execute(connection, $"INSERT OR REPLACE INTO meta (key, value) VALUES ('schema', '{SchemaVersion}')");

        _built = Scalar(connection, "SELECT value FROM meta WHERE key = 'built'") == "1";

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, file, size, modified, subject, sender, priority, attachments, is_read FROM messages";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var from = JsonSerializer.Deserialize<List<IndexedAddress>>(reader.GetString(5)) ?? [];

            _rows[reader.GetString(1)] = new Row(
                reader.GetInt64(0),
                reader.GetInt64(2),
                reader.GetInt64(3),
                new MessageSummary(
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    from,
                    reader.GetString(6),
                    reader.GetInt32(7),
                    reader.GetInt64(8) != 0));
        }
    }

    SqliteConnection Connect()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    static string? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar() as string;
    }

    static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    static bool IsNetworkPath(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (full.StartsWith(@"\\", StringComparison.Ordinal)) return true;

            var root = Path.GetPathRoot(full);
            return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch (Exception)
        {
            return false;
        }
    }

    #endregion

    public void Dispose() => _writeLock.Dispose();
}
