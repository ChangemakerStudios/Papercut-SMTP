namespace Papercut.Core.Infrastructure.Plugins
{
    using System.Collections.Generic;

    public class PluginModuleEqualityComparer : IEqualityComparer<IPluginModule>
    {
        public bool Equals(IPluginModule x, IPluginModule y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return string.Equals(x.Name, y.Name) && string.Equals(x.Version, y.Version) && string.Equals(x.Description, y.Description);
        }

        public int GetHashCode(IPluginModule obj)
        {
            unchecked
            {
                var hashCode = obj.Name?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (obj.Version?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.Description?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public static IEqualityComparer<IPluginModule> Instance { get; } = new PluginModuleEqualityComparer();
    }
}