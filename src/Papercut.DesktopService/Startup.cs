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

namespace Papercut.DesktopService
{
    using System;
    using System.Threading.Tasks;
    using Papercut.Service;
    using Autofac;

    // entry point for Desktop Electron Edge
    public class Startup
    {
        public Task<object> Invoke(object input)
        {
            var _ = Task.Factory.StartNew(() => {
                Program.Main(new string[0]);
                PapercutNativeMessageRepository.HandlerInstance = Program.AppContainer.Resolve<PapercutNativeMessageRepository>();
            });
            return Task.FromResult((object)((Func<object, Task<object>>)Stop));
        }

        static Task<object> Stop(object input){
            Program.Exit();
            return Task.FromResult((object)0);
        }
    }
}