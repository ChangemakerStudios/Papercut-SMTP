namespace Papercut.SMTP
{
    /// <summary>
    /// The Server interface.
    /// </summary>
    public interface IServer
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The start.
        /// </summary>
        void Bind(string ip, int port);

        /// <summary>
        ///     The stop.
        /// </summary>
        void Stop();

        #endregion
    }
}