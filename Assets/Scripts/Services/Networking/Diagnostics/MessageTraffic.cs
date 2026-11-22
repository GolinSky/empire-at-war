namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>One message type's traffic in the current one-second window (message bytes, no transport headers).</summary>
    public sealed class MessageTraffic
    {
        public MessageTraffic(string name) => Name = name;

        public string Name { get; }
        public int InMessages { get; private set; }
        public long InBytes { get; private set; }
        public int OutMessages { get; private set; }
        public long OutBytes { get; private set; }

        public void AddIn(int messages, long bytes)
        {
            InMessages += messages;
            InBytes += bytes;
        }

        public void AddOut(int messages, long bytes)
        {
            OutMessages += messages;
            OutBytes += bytes;
        }

        public void ResetWindow()
        {
            InMessages = 0;
            InBytes = 0;
            OutMessages = 0;
            OutBytes = 0;
        }
    }
}
