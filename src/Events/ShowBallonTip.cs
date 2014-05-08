namespace Papercut.Events
{
    using System.Windows.Forms;

    using Papercut.Core.Events;

    public class ShowBallonTip : IDomainEvent
    {
        public int Timeout { get; set; }
        public string TipTitle { get; set; }
        public string TipText { get; set; }
        public ToolTipIcon ToolTipIcon { get; set; }

        public ShowBallonTip(int timeout, string tipTitle, string tipText, ToolTipIcon toolTipIcon)
        {
            Timeout = timeout;
            TipTitle = tipTitle;
            TipText = tipText;
            ToolTipIcon = toolTipIcon;
        }
    }
}