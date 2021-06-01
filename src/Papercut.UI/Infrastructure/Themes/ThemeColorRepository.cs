// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;

    using Autofac;

    using Papercut.Core.Annotations;

    public class ThemeColorRepository
    {
        private static List<ThemeColor> ThemeColors { get; } = typeof(Colors)
            .GetProperties()
            .Select(p => new ThemeColor(p.Name, (Color)p.GetValue(null)))
            .ToList();

        public IReadOnlyCollection<ThemeColor> GetAll() => ThemeColors;

        [CanBeNull]
        public ThemeColor FirstOrDefaultByName(string name)
        {
            return this.GetAll().FirstOrDefault(
                s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                     || s.Description.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #region Begin Static Container Registrations

        static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<ThemeColorRepository>().AsSelf().InstancePerLifetimeScope();
        }

        #endregion
    }
}