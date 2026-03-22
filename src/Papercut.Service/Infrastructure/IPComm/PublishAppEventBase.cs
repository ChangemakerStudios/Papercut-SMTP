// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


namespace Papercut.Service.Infrastructure.IPComm;

public abstract class PublishAppEventBase(
    PapercutIPCommClientFactory ipCommClientFactory,
    ILogger logger)
{
    protected abstract PapercutIPCommClientConnectTo ConnectTo { get; }

    protected virtual TimeSpan PublishTimeout { get; } = TimeSpan.FromMilliseconds(500);

    public virtual async Task PublishAsync<T>(T @event, CancellationToken token = default)
        where T : IEvent
    {
        var ipCommClient = ipCommClientFactory.GetClient(ConnectTo);

        try
        {
            logger.Information(
                "Publishing {EventName} to the Papercut {ConnectTo}",
                @event.GetType().Name,
                ConnectTo);

            await ipCommClient.PublishEventServer(@event, PublishTimeout, token);
        }
        catch (Exception ex) when (ex is TaskCanceledException or TimeoutException)
        {
        }
        catch (Exception ex)
        {
            logger.Information(
                ex,
                "Failed to publish {Endpoint} specified. Papercut {ConnectTo} is most likely not running.",
                ipCommClient.Endpoint, ConnectTo);
        }
    }
}