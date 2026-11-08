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

/** Rule type discriminators — the IRule.Type display strings. */
export type RuleType =
  | 'Relay'
  | 'Forward'
  | 'Conditional Forward'
  | 'Conditional Forward with Retry'
  | 'Invoke Process'
  | 'Cleanup Mail';

export const RULE_TYPES: RuleType[] = [
  'Forward',
  'Relay',
  'Conditional Forward',
  'Conditional Forward with Retry',
  'Invoke Process',
  'Cleanup Mail'
];

/**
 * Flat union of all rule type properties, discriminated by `type`.
 * Matches the C# RulesController.RuleDto.
 */
export interface RuleDto {
  id?: string | null;
  type: RuleType;
  name?: string | null;
  isEnabled: boolean;
  description?: string | null;

  // Relay + Forward family
  smtpServer?: string | null;
  smtpPort?: number | null;
  smtpUseSSL?: boolean | null;
  smtpUsername?: string | null;
  smtpPassword?: string | null;
  toBcc?: string | null;
  fromEmail?: string | null;
  toEmail?: string | null;

  // Conditional
  regexHeaderMatch?: string | null;
  regexBodyMatch?: string | null;

  // Retry
  retryAttempts?: number | null;
  retryAttemptDelaySeconds?: number | null;

  // Invoke Process
  processToRun?: string | null;
  processCommandLine?: string | null;

  // Mail Retention
  mailRetentionDays?: number | null;
}

const RELAY_FAMILY: RuleType[] = ['Relay', 'Forward', 'Conditional Forward', 'Conditional Forward with Retry'];
const FORWARD_FAMILY: RuleType[] = ['Forward', 'Conditional Forward', 'Conditional Forward with Retry'];
const CONDITIONAL_FAMILY: RuleType[] = ['Conditional Forward', 'Conditional Forward with Retry'];

export function isRelayFamily(type: RuleType): boolean {
  return RELAY_FAMILY.includes(type);
}

export function isForwardFamily(type: RuleType): boolean {
  return FORWARD_FAMILY.includes(type);
}

export function isConditionalFamily(type: RuleType): boolean {
  return CONDITIONAL_FAMILY.includes(type);
}

export function newRule(type: RuleType): RuleDto {
  const base: RuleDto = { type, isEnabled: true, name: '' };

  if (isRelayFamily(type)) {
    base.smtpServer = '';
    base.smtpPort = 25;
    base.smtpUseSSL = false;
  }

  if (type === 'Conditional Forward with Retry') {
    base.retryAttempts = 3;
    base.retryAttemptDelaySeconds = 60;
  }

  if (type === 'Cleanup Mail') {
    base.mailRetentionDays = 30;
  }

  return base;
}
