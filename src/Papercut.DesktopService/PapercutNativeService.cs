using System;
using System.Threading.Tasks;

namespace Papercut.DesktopService
{
    class PapercutNativeService
    {
        internal static PapercutNativeMessageRepository MailMessageRepo { get; set; }

        static async Task<object> ListAll(object input)
        {
            var paramters = input?.ToString();
            return await MailMessageRepo.ListAll(paramters);
        }

        static async Task<object> DeleteAll(object input)
        {
            return await MailMessageRepo.DeleteAll();
        }

        static async Task<object> GetDetail(object input)
        {
            var id = input?.ToString();
            return await MailMessageRepo.GetDetail(id);
        }

        static async Task<object> OnNewMessageArrives(object input)
        {
            var callback = input as Func<object, Task<object>>;
            if (callback != null)
            {
                return await MailMessageRepo.OnNewMessageArrives(async (ev) =>
                {
                    await callback(ev);
                });
            }

            return await Task.FromResult((object)0);
        }

        public static object ExportAll() {
            return new
            {
                ListAll = (Func<object, Task<object>>)PapercutNativeService.ListAll,
                DeleteAll = (Func<object, Task<object>>)PapercutNativeService.DeleteAll,
                GetDetail = (Func<object, Task<object>>)PapercutNativeService.GetDetail,
                OnNewMessageArrives = (Func<object, Task<object>>)PapercutNativeService.OnNewMessageArrives
            };
        }
    }
}
