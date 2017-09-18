// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Module.Seq
{
    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Infrastructure.Plugins;

    public class SeqLoggingModule : Module, IPluginModule
    {
        public string Name => "Papercut Seq Logging";
        public string Version => "1.0.0.0";
        public string Description => "Papercut Serilog to Seq (http://getseq.net) local instance";
        public IModule Module => this;

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AddSeqToConfiguration>().AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}