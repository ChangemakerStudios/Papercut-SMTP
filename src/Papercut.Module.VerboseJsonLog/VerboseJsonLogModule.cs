// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2018 Jaben Cargman
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
namespace Papercut.Module.VerboseJsonLog
{
    using System;

    using Autofac;
    using Autofac.Core;

    using Papercut.Common.Helper;
    using Papercut.Core.Infrastructure.Plugins;

    public class VerboseJsonLogModule : Module, IPluginModule
    {
        public string Name => "Verbose Json Logging";

        public string Version => this.ThisAssembly.GetVersion();

        public string Description => "Verbose (JSON) Serilog Logging Support for Papercut";

        public IModule Module => this;

        public Guid Id => new Guid("97565ED4-A676-4B08-A56B-7E58B24A4FA6");

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SetupVerboseJsonLoggingHandler>().AsImplementedInterfaces();
        }
    }
}