// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.Rules
{
    using System;
    using System.Reflection;

    using Autofac;
    using Autofac.Core;

    using Papercut.Core.Domain.Rules;

    using Module = Autofac.Module;

    public class PapercutRuleModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // rules and rule dispatchers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IRule>()
                .As<IRule>()
                .InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AsClosedTypesOf(typeof(IRuleDispatcher<>))
                .AsImplementedInterfaces()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<RulesRunner>().As<IRulesRunner>().SingleInstance();

            builder.RegisterType<RuleRepository>().AsSelf().SingleInstance();
        }
    }
}