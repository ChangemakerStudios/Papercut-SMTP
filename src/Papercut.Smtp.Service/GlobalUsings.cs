// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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

global using Papercut.Core;
global using Papercut.Core.Annotations;
global using Papercut.Core.Domain.Message;
global using Papercut.Core.Domain.Rules;
global using Papercut.Core.Domain.Application;
global using Papercut.Core.Domain.Network;
global using Papercut.Core.Domain.Network.Smtp;
global using Papercut.Core.Domain.Settings;
global using Papercut.Core.Domain.Paths;
global using Papercut.Core.Infrastructure;
global using Papercut.Core.Infrastructure.Logging;
global using Papercut.Core.Infrastructure.Lifecycle;
global using Papercut.Core.Infrastructure.Network;

global using Papercut.Core.Infrastructure.MessageBus;

global using Papercut.Rules;

global using Papercut.Infrastructure.IPComm;
global using Papercut.Infrastructure.Smtp;

global using Papercut.Common.Domain;
global using Papercut.Common.Extensions;

global using Papercut.Message;

global using Papercut.Smtp.Service.Helpers;
global using Papercut.Smtp.Service.Models;

global using Autofac.Extensions.DependencyInjection;
global using Autofac;
global using Autofac.Core;

global using MimeKit;

global using Serilog;
global using ILogger = Serilog.ILogger;

global using Microsoft.AspNetCore.Mvc;

global using System.Diagnostics;
global using System.Reflection;

global using System.Reactive.Concurrency;
global using System.Reactive.Linq;

global using System.Collections.ObjectModel;
global using System.Collections.Concurrent;