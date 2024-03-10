// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using Microsoft.Extensions.Caching.Memory;

namespace Papercut.Message;

public class MimeMessageLoader(MessageRepository messageRepository, ILogger logger)
{
    public static MemoryCache _mimeMessageCache;

    readonly ILogger _logger = logger.ForContext<MimeMessageLoader>();

    static MimeMessageLoader()
    {
        _mimeMessageCache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<MimeMessage?> Get(MessageEntry messageEntry)
    {
        this._logger.Verbose("Loading Message Entry {@MessageEntry}", messageEntry);

        try
        {
            return await this.GetMimeMessageFromCache(messageEntry);
        }
        catch (OperationCanceledException)
        {
            // no need to respond...
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Exception Loading {@MessageEntry}", messageEntry);
        }

        return null;
    }

    private async Task<MimeMessage?> GetMimeMessageFromCache(MessageEntry messageEntry)
    {
        if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));
        if (messageEntry.File == null) throw new ArgumentNullException(nameof(messageEntry.File));

        return await _mimeMessageCache.GetOrCreateAsync(
            messageEntry.File,
            async cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(3);

                this._logger.Verbose("Getting Message Data from Message Repository");

                var messageData = await messageRepository.GetMessage(messageEntry);

                if (messageData == null) return null;

                using var ms = new MemoryStream(messageData);

                this._logger.Verbose(
                    "MimeMessage Load for {@MessageEntry}",
                    messageEntry);

                return await MimeMessage.LoadAsync(ParserOptions.Default, ms);
            });
    }
}