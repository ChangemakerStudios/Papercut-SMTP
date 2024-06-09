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


using Microsoft.Extensions.Configuration;

using Serilog.Configuration;

namespace Papercut.Service.Infrastructure.Logging;

public class AddReadFromConfiguration(IConfiguration configuration) : ILoggerSettings
{
    public void Configure(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration.ReadFrom.Configuration(configuration);
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<AddReadFromConfiguration>().As<ILoggerSettings>();
    }

    #endregion
}