namespace Papercut.Message.Helpers
{
    using System;
    using System.Reactive.Threading.Tasks;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;

    public static class MimeMessageLoaderExtensions
    {
        public static MimeMessage LoadMailMessage([NotNull] this MimeMessageLoader loader, [NotNull] MessageEntry entry)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var loadTask = loader.Get(entry).ToTask();
            loadTask.Wait();

            return loadTask.Result;
        }
    }
}