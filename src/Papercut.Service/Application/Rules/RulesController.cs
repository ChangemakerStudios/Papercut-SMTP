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


using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Cleanup;
using Papercut.Rules.Domain.Conditional.Forwarding;
using Papercut.Rules.Domain.Forwarding;
using Papercut.Rules.Domain.Invoking;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Service.Application.Rules;

/// <summary>
///     Rules management, replacing the desktop's IPComm rules sync. GET
///     returns the active rule set; PUT replaces it wholesale (the same
///     semantics as the IPComm <see cref="RulesUpdatedEvent" /> path) and
///     the singleton RuleService persists and applies it immediately.
/// </summary>
[Route("api/[controller]")]
public class RulesController(Infrastructure.Rules.RuleService ruleService, IMessageBus messageBus) : ControllerBase
{
    [HttpGet]
    public IEnumerable<RuleDto> Get()
    {
        return ruleService.Rules.Select(RuleDto.CreateFrom);
    }

    [HttpPut]
    public async Task<ActionResult<IEnumerable<RuleDto>>> Update([FromBody] List<RuleDto> rules, CancellationToken token = default)
    {
        IRule[] mapped;

        try
        {
            mapped = rules.Select(r => r.ToRule()).ToArray();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        await messageBus.PublishAsync(new RulesUpdatedEvent(mapped), token);

        return Ok(ruleService.Rules.Select(RuleDto.CreateFrom));
    }

    /// <summary>
    ///     Flat union of all rule type properties, discriminated by
    ///     <see cref="Type" /> (the IRule.Type display strings).
    /// </summary>
    [PublicAPI]
    public class RuleDto
    {
        public Guid? Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? Name { get; set; }

        public bool IsEnabled { get; set; } = true;

        public string? Description { get; set; }

        // Relay + Forward family
        public string? SmtpServer { get; set; }

        public int? SmtpPort { get; set; }

        public bool? SmtpUseSSL { get; set; }

        public string? SmtpUsername { get; set; }

        public string? SmtpPassword { get; set; }

        public string? ToBcc { get; set; }

        public string? FromEmail { get; set; }

        public string? ToEmail { get; set; }

        // Conditional
        public string? RegexHeaderMatch { get; set; }

        public string? RegexBodyMatch { get; set; }

        // Retry
        public int? RetryAttempts { get; set; }

        public int? RetryAttemptDelaySeconds { get; set; }

        // Invoke Process
        public string? ProcessToRun { get; set; }

        public string? ProcessCommandLine { get; set; }

        // Mail Retention
        public int? MailRetentionDays { get; set; }

        public static RuleDto CreateFrom(IRule rule)
        {
            var dto = new RuleDto
            {
                Id = rule.Id,
                Type = rule.Type,
                Name = rule.Name,
                IsEnabled = rule.IsEnabled,
                Description = rule.Description
            };

            if (rule is RelayRule relay)
            {
                dto.SmtpServer = relay.SmtpServer;
                dto.SmtpPort = relay.SmtpPort;
                dto.SmtpUseSSL = relay.SmtpUseSSL;
                dto.SmtpUsername = relay.SmtpUsername;
                dto.SmtpPassword = relay.SmtpPassword;
                dto.ToBcc = relay.ToBcc;
            }

            if (rule is ForwardRule forward)
            {
                dto.FromEmail = forward.FromEmail;
                dto.ToEmail = forward.ToEmail;
            }

            if (rule is ConditionalForwardRule conditional)
            {
                dto.RegexHeaderMatch = conditional.RegexHeaderMatch;
                dto.RegexBodyMatch = conditional.RegexBodyMatch;
            }

            if (rule is ConditionalForwardWithRetryRule retry)
            {
                dto.RetryAttempts = retry.RetryAttempts;
                dto.RetryAttemptDelaySeconds = retry.RetryAttemptDelaySeconds;
            }

            if (rule is InvokeProcessRule invoke)
            {
                dto.ProcessToRun = invoke.ProcessToRun;
                dto.ProcessCommandLine = invoke.ProcessCommandLine;
            }

            if (rule is MailRetentionRule retention)
            {
                dto.MailRetentionDays = retention.MailRetentionDays;
            }

            return dto;
        }

        public IRule ToRule()
        {
            IRule rule = Type switch
            {
                "Relay" => this.PopulateRelay(new RelayRule()),
                "Forward" => this.PopulateForward(new ForwardRule()),
                "Conditional Forward" => this.PopulateConditional(new ConditionalForwardRule()),
                "Conditional Forward with Retry" => this.PopulateRetry(new ConditionalForwardWithRetryRule()),
                "Invoke Process" => new InvokeProcessRule
                {
                    ProcessToRun = this.ProcessToRun,
                    ProcessCommandLine = this.ProcessCommandLine
                },
                "Cleanup Mail" => new MailRetentionRule
                {
                    MailRetentionDays = this.MailRetentionDays ?? 30
                },
                _ => throw new ArgumentException($"Unknown rule type '{Type}'")
            };

            rule.Name = this.Name ?? string.Empty;
            rule.IsEnabled = this.IsEnabled;

            return rule;
        }

        private RelayRule PopulateRelay(RelayRule rule)
        {
            rule.SmtpServer = this.SmtpServer ?? string.Empty;
            rule.SmtpPort = this.SmtpPort ?? 25;
            rule.SmtpUseSSL = this.SmtpUseSSL ?? false;
            rule.SmtpUsername = this.SmtpUsername ?? string.Empty;
            rule.SmtpPassword = this.SmtpPassword ?? string.Empty;
            rule.ToBcc = this.ToBcc ?? string.Empty;
            return rule;
        }

        private ForwardRule PopulateForward(ForwardRule rule)
        {
            this.PopulateRelay(rule);
            rule.FromEmail = this.FromEmail ?? string.Empty;
            rule.ToEmail = this.ToEmail ?? string.Empty;
            return rule;
        }

        private ConditionalForwardRule PopulateConditional(ConditionalForwardRule rule)
        {
            this.PopulateForward(rule);
            rule.RegexHeaderMatch = this.RegexHeaderMatch ?? string.Empty;
            rule.RegexBodyMatch = this.RegexBodyMatch ?? string.Empty;
            return rule;
        }

        private ConditionalForwardWithRetryRule PopulateRetry(ConditionalForwardWithRetryRule rule)
        {
            this.PopulateConditional(rule);
            rule.RetryAttempts = this.RetryAttempts ?? 3;
            rule.RetryAttemptDelaySeconds = this.RetryAttemptDelaySeconds ?? 60;
            return rule;
        }
    }
}
