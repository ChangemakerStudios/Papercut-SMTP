namespace Papercut.Core.Events
{
    public class AppForceShutdownEvent : IDomainEvent
    {
        public int ExitCode { get; set; }

        public AppForceShutdownEvent(int exitCode = 0)
        {
            ExitCode = exitCode;
        }
    }
}