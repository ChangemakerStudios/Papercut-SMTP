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


global using System.Diagnostics;
global using System.IO;
global using System.Reactive.Concurrency;
global using System.Reactive.Linq;
global using System.Reflection;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Threading;

global using Autofac;
global using Autofac.Util;

global using Caliburn.Micro;

global using MahApps.Metro.Controls;

global using MimeKit;

global using Papercut.Common.Domain;
global using Papercut.Common.Extensions;
global using Papercut.Common.Helper;
global using Papercut.Domain.Events;
global using Papercut.Domain.LifecycleHooks;
global using Papercut.Helpers;
global using Papercut.Properties;