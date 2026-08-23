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


namespace Papercut.Infrastructure.Smtp.RateLimiting;

/// <summary>
/// The outcome of a single <see cref="SmtpRateLimiter.TryAcquire" /> attempt.
/// </summary>
/// <param name="IsAllowed">Whether the message may be accepted.</param>
/// <param name="Count">Messages accepted in the current window, including this one when allowed.</param>
/// <param name="RetryAfter">Time remaining until the window resets. Zero when allowed.</param>
public readonly record struct RateLimitDecision(bool IsAllowed, int Count, TimeSpan RetryAfter);
