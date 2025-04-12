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


using Autofac.Builder;

namespace Papercut.Core.Infrastructure.Container;

public static class AutofacRegistrationExtensions
{
    /// <summary>
    /// Single instance per UI scope
    /// </summary>
    /// <typeparam name="TLimit"></typeparam>
    /// <typeparam name="TActivatorData"></typeparam>
    /// <typeparam name="TRegistrationStyle"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> InstancePerUIScope<TLimit, TActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> builder)
    {
        return builder.InstancePerMatchingLifetimeScope(ContainerScope.UIScopeTag);
    }
}