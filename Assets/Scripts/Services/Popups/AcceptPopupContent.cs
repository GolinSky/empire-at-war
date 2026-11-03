namespace EmpireAtWar.Services.Popups
{
    public sealed class AcceptPopupContent
    {
        public string Title { get; }
        public string Message { get; }
        public string AcceptLabel { get; }
        public string DeclineLabel { get; }

        public AcceptPopupContent(string title, string message, string acceptLabel = "Accept",
            string declineLabel = "Decline")
        {
            Title = title;
            Message = message;
            AcceptLabel = acceptLabel;
            DeclineLabel = declineLabel;
        }
    }
}
