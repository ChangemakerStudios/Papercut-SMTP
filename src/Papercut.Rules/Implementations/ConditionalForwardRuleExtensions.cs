// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Rules.Implementations
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using MimeKit;

    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;

    public static class ConditionalForwardRuleExtensions
    {
        public static bool IsConditionalForwardRuleMatch([NotNull] this ConditionalForwardRule rule, [NotNull] MimeMessage mimeMessage)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (mimeMessage == null) throw new ArgumentNullException(nameof(mimeMessage));

            if (rule.RegexHeaderMatch.IsSet())
            {
                var allHeaders = string.Join("\r\n", mimeMessage.Headers.Select(h => h.ToString()));

                if (!IsMatch(rule.RegexHeaderMatch, allHeaders))
                    return false;
            }

            if (rule.RegexBodyMatch.IsSet())
            {
                var bodyText = string.Join("\r\n",
                    mimeMessage.BodyParts.OfType<TextPart>().Where(s => !s.IsAttachment));

                if (!IsMatch(rule.RegexBodyMatch, bodyText))
                    return false;
            }

            return true;
        }

        private static bool IsMatch(string match, string searchText)
        {
            var regex = new Regex(match, RegexOptions.IgnoreCase);
            return regex.IsMatch(searchText);
        }
    }
}