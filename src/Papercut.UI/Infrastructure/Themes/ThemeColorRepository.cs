// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2025 Jaben Cargman
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


using System.Windows.Media;

using Papercut.AppLayer.Themes;
using Papercut.Domain.Themes;

namespace Papercut.Infrastructure.Themes;

public class ThemeColorRepository
{
    public const string SystemThemeName = "System";

    private static readonly List<ThemeColor> NamedColors = typeof(Colors)
        .GetProperties()
        .Where(s => !s.Name.Equals("Transparent"))
        .Select(p => new ThemeColor(p.Name, (Color)p.GetValue(null)!))
        .ToList();

    private static Color GetSystemAccentColorOrDefault()
    {
        return SystemThemeRegistryHelper.GetSystemAccentColor() ?? Colors.SteelBlue;
    }

    public IReadOnlyCollection<ThemeColor> GetAll()
    {
        var colors = new List<ThemeColor>(NamedColors.Count + 1)
        {
            new(SystemThemeName, GetSystemAccentColorOrDefault())
        };
        colors.AddRange(NamedColors);
        return colors;
    }

    public static readonly ThemeColor Default = new(SystemThemeName, GetSystemAccentColorOrDefault());

    public ThemeColor? FirstOrDefaultByName(string nameOrDescription)
    {
        var name = nameOrDescription.Replace(" ", string.Empty).Trim();

        return GetAll().FirstOrDefault(
            s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                 || s.Description.Equals(nameOrDescription, StringComparison.OrdinalIgnoreCase));
    }

    public Color ResolveAccentColor(string themeName)
    {
        if (themeName.Equals(SystemThemeName, StringComparison.OrdinalIgnoreCase))
        {
            return GetSystemAccentColorOrDefault();
        }

        return FirstOrDefaultByName(themeName)?.Color ?? Default.Color;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<ThemeColorRepository>().AsSelf().InstancePerLifetimeScope();
    }

    #endregion
}