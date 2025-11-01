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


using Autofac;

using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;
using Papercut.Message;
using Papercut.Rules.Domain.Cleanup;

namespace Papercut.Rules.App.Cleanup;

[UsedImplicitly]
public class MailRetentionRuleDispatch : IRuleDispatcher<MailRetentionRule>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger _logger;

    public MailRetentionRuleDispatch(IMessageRepository messageRepository, ILogger logger)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(MailRetentionRule rule, MessageEntry? messageEntry = null, CancellationToken token = default)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        if (rule.MailRetentionDays <= 0)
        {
            _logger.Warning("MailRetentionRule has invalid retention days: {RetentionDays}. Skipping cleanup.", rule.MailRetentionDays);
            return;
        }

        var cutoffDate = DateTime.Now.AddDays(-rule.MailRetentionDays);

        _logger.Debug("Starting mail cleanup for messages older than {CutoffDate} (retention: {RetentionDays} days)",
            cutoffDate, rule.MailRetentionDays);

        var messages = _messageRepository.LoadMessages().ToList();
        var messagesToDelete = messages
            .Where(m => m.ModifiedDate < cutoffDate)
            .ToList();

        if (messagesToDelete.Count == 0)
        {
            _logger.Debug("No messages found older than {CutoffDate} to delete", cutoffDate);
            return;
        }

        _logger.Information("Found {Count} messages to delete (older than {CutoffDate})",
            messagesToDelete.Count, cutoffDate);

        int deletedCount = 0;
        int failedCount = 0;

        foreach (var message in messagesToDelete)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                if (_messageRepository.DeleteMessage(message))
                {
                    deletedCount++;
                    _logger.Debug("Deleted message: {MessageFile}", message.File);
                }
                else
                {
                    failedCount++;
                    _logger.Warning("Failed to delete message (file not found): {MessageFile}", message.File);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.Error(ex, "Error deleting message: {MessageFile}", message.File);
            }
        }

        _logger.Information("Mail cleanup completed: {DeletedCount} deleted, {FailedCount} failed",
            deletedCount, failedCount);

        await Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<MailRetentionRuleDispatch>()
            .As<IRuleDispatcher<MailRetentionRule>>().AsSelf().InstancePerDependency();
    }

    #endregion
}
