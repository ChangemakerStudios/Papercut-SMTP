using System;
using System.Threading.Tasks;

namespace Papercut.Service
{
    // entry point for Desktop Electron Edge
    public class Startup
    {
        public async Task<object> Invoke(object input)
        {
            var _ = Task.Factory.StartNew(() => {
                Program.Main(new string[0]);
            });
            return (object)((Func<object, Task<object>>)Stop);
        }

        static async Task<object> Stop(object input){
            Program.Exit();
            return 0;
        }
    }
}