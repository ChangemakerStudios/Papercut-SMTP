﻿// Papercut
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


using Caliburn.Micro;

using Papercut.ViewModels;

namespace Papercut.Helpers
{
    public static class MessageDetailItemHelper
    {
        public static Conductor<IMessageDetailItem>.Collection.OneActive GetConductor<T>(this T messageDetailItem)
            where T : Screen, IMessageDetailItem
        {
            return messageDetailItem.Parent as Conductor<IMessageDetailItem>.Collection.OneActive;
        }

        public static async Task<T> ActivateViewModelOf<T>(
            this Conductor<IMessageDetailItem>.Collection.OneActive conductor)
        {
            ArgumentNullException.ThrowIfNull(conductor);

            var item = conductor?.Items.FirstOrDefault(s => s.GetType() == typeof(T));

            if (item != null)
            {
                await conductor.ActivateItemAsync(item);
                return (T)item;
            }

            return default(T);
        }
    }
}