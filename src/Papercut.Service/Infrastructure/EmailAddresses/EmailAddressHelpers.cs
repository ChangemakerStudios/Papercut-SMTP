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


using Papercut.Service.Domain.Models;

namespace Papercut.Service.Infrastructure.EmailAddresses;

public static class EmailAddressHelpers
{
    public static List<EmailAddressDto> ToAddressList(this IEnumerable<InternetAddress>? mailAddresses)
    {
        if (mailAddresses == null)
        {
            return [];
        }

        return mailAddresses
            .IfNullEmpty()
            .OfType<MailboxAddress>()
            .Select(f => new EmailAddressDto { Address = f.Address, Name = f.Name })
            .ToList();
    }
}