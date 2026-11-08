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

namespace Papercut.Message;

/// <summary>
/// Remembers message files that have already been announced, so the same file is
/// not published twice.
///
/// Two paths legitimately notice a new message: the SMTP handler publishes as soon
/// as it saves the file, and the file watcher publishes when a file appears in a
/// watched folder. For SMTP mail both fire, which meant rules ran twice and every
/// client saw the message twice.
///
/// The SMTP publish is the one kept, because it is immediate and always happens --
/// the save folder is not guaranteed to be watched (it is created on demand and
/// only pre-existing folders get a watcher), so the watcher alone would miss
/// messages on a fresh install.
/// </summary>
public class PublishedMessageTracker
{
    /// <summary>
    /// How long a claim is honoured. The watcher waits for the file to become
    /// readable before it publishes, retrying with growing delays, so this has to
    /// comfortably outlast that -- otherwise the claim expires before the watcher
    /// checks it and the duplicate comes back.
    /// </summary>
    private static readonly TimeSpan ClaimLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Match the file system: Windows and macOS compare paths case-insensitively,
    /// Linux (where the service commonly runs in Docker) does not, and there
    /// "mail.eml" and "MAIL.eml" are different messages.
    /// </summary>
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private readonly ConcurrentDictionary<string, DateTime> _published = new(PathComparer);

    /// <summary>
    /// Records that <paramref name="messageFilePath" /> is being published by the
    /// caller. Call this before publishing, so a watcher that reacts immediately
    /// still sees the claim.
    /// </summary>
    public void MarkPublished(string messageFilePath)
    {
        if (string.IsNullOrWhiteSpace(messageFilePath)) return;

        Prune();

        _published[Normalize(messageFilePath)] = DateTime.UtcNow;
    }

    /// <summary>
    /// True when this file was already published by someone else, in which case the
    /// caller should stay quiet. The claim is consumed, so a genuinely new message
    /// that later reuses the path is still announced.
    /// </summary>
    public bool ClaimAlreadyPublished(string messageFilePath)
    {
        if (string.IsNullOrWhiteSpace(messageFilePath)) return false;

        if (!_published.TryRemove(Normalize(messageFilePath), out var publishedAt)) return false;

        // a stale entry means the original publish was long ago; treat this as new
        return DateTime.UtcNow - publishedAt <= ClaimLifetime;
    }

    private void Prune()
    {
        var cutoff = DateTime.UtcNow - ClaimLifetime;

        foreach (var entry in _published)
        {
            // remove only the exact entry observed, so a claim re-made for the same
            // path while pruning is not swept away with the stale one
            if (entry.Value < cutoff) _published.TryRemove(entry);
        }
    }

    private static string Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }
}
