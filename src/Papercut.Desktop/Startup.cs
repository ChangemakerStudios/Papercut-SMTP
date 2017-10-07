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

namespace Papercut.Desktop
{
    using System;
    using System.Threading.Tasks;
    using System.Reflection;
    using Papercut.DesktopService;
    using System.Threading;

    // entry point for Desktop Electron Edge
    public class Startup
    {
        public async Task<object> Invoke(object input)
        {
            var waitForDebugger = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEBUG_PAPERCUT"));
            if (waitForDebugger)
            {
                Console.WriteLine("Waiting 30s for debugger...");
                Thread.Sleep(30 * 1000);
            }

            var entryPointAssembly = (typeof(Startup).GetTypeInfo()).Assembly;
            return await NativeService.StartPapercut(entryPointAssembly);
        }
    }
}