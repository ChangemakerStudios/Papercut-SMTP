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


namespace Papercut.Infrastructure.IPComm
{
    using System;
    using System.Threading.Tasks;

    using Network;

    public static class ConnectionExtensions
    {
        public static async Task SendString(this Connection connection, string message)
        {
            await connection.SendData(connection.Encoding.GetBytes(message));
        }

        public static async Task SendLine(this Connection connection, string message)
        {
            await connection.SendString($"{message}{Environment.NewLine}");
        }

        public static async Task SendJson(this Connection connection, Type type, object instance)
        {
            var json = PapercutIPCommSerializer.ToJson(type, instance);

            await connection.SendData(connection.Encoding.GetBytes(json));
        }

        public static async Task Send(
            this Connection connection,
            string message,
            params object[] args)
        {
            await connection.SendData(connection.Encoding.GetBytes(string.Format(message, args)));
        }
    }
}