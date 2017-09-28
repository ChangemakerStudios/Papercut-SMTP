

namespace Papercut.DesktopService
{
    using Papercut.Common.Domain;
    using Papercut.WebUI;

    public class PapercutNativeMessageRepository: IEventHandler<WebUIServerReadyEvent>
    {

        public void Handle(WebUIServerReadyEvent @event)
        {
            
        }
    }
}