using System;
using System.Threading.Tasks;

namespace Papercut.Service
{
    // entry point for Desktop Electron Edge
    public class Startup
    {
        public async Task<object> Invoke(object input)
        {
            Task.Factory.StartNew(() => {
                Program.Main(new string[0]);
            });
            return await Task.FromResult((object)CreateStopTask());
        }

        static Func<object, Task<object>> CreateStopTask(){
            return (async (object o) => {
                Program.Exit();
                return await Task.FromResult((object)0);
            });
        }
    }
}