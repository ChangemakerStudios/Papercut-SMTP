namespace Papercut.Core.Plugins
{
    using System.Collections.Generic;
    using Autofac.Core;

    public interface IPluginMeta
    {
        string Name { get; }

        string Version { get; }

        string Description { get; }
    }

    public interface IPluginModule : IPluginMeta
    {
        IEnumerable<IModule> Modules { get; }
    }
}