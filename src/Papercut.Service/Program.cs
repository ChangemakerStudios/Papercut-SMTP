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

namespace Papercut.Service
{
    using System;

    using Serilog;

    class Program
    {
        static RunServiceApp app;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, a) =>
            {
                if (Log.Logger == null) return;
                if (a.IsTerminating) Log.Logger.Fatal(a.ExceptionObject as Exception, "Unhandled Exception");
                else
                {
                    Log.Logger.Information(
                        a.ExceptionObject as Exception,
                        "Non-Fatal Unhandled Exception");
                }
            };

            app = new RunServiceApp();
            app.Run();
        }
    }
}