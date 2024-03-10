// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2024 Jaben Cargman
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


namespace Papercut.Message.Helpers;

public static class MimeMessageLoaderExtensions
{
    public static async Task<MimeMessage> LoadMailMessage(this MimeMessageLoader loader, MessageEntry entry)
    {
        if (loader == null) throw new ArgumentNullException(nameof(loader));
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        return await loader.Get(entry);
    }
}