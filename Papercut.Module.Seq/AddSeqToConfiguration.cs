namespace Papercut.Module.Seq
{
    using global::Seq;

    using Papercut.Core.Events;

    using Serilog.Events;

    public class AddSeqToConfiguration : IHandleEvent<ConfigureLoggerEvent>
    {
        #region Public Methods and Operators

        public void Handle(ConfigureLoggerEvent @event)
        {
            @event.LogConfiguration.WriteTo.Seq("http://localhost:5341", LogEventLevel.Verbose);
        }

        #endregion
    }
}