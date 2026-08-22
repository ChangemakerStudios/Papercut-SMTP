// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Features;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.DependencyInjection;

global using MimeKit;

global using Papercut.Common.Domain;
global using Papercut.Common.Extensions;
global using Papercut.Core.Domain.Application;
global using Papercut.Core.Domain.Message;
global using Papercut.Core.Domain.Paths;
global using Papercut.Core.Domain.Settings;
global using Papercut.Core.Infrastructure.Container;
global using Papercut.Core.Infrastructure.Lifecycle;
global using Papercut.Infrastructure.IPComm;
global using Papercut.Infrastructure.Smtp;
global using Papercut.Message;
global using Papercut.Service.Domain.SmtpServer;

global using Autofac;

global using Module = Autofac.Module;