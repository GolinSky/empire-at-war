namespace EmpireAtWar.Services.BattleNotifications
{
    public sealed class BattleNotification
    {
        private readonly string _text;
        private readonly string _groupFormat;

        public int Id { get; }
        public BattleNotificationPriority Priority { get; }
        public string GroupKey { get; }
        public int Count { get; private set; } = 1;
        public float TimeLeft { get; private set; }
        /// <summary>Seconds since the card was posted or last merged; merging stops after the group window.</summary>
        public float SinceLastMerge { get; private set; }

        public string Text => Count == 1 ? _text
            : _groupFormat != null ? string.Format(_groupFormat, Count) : $"{_text} ×{Count}";

        public BattleNotification(int id, BattleNotificationPriority priority, string text, string groupKey,
            string groupFormat, float lifetime)
        {
            Id = id;
            Priority = priority;
            _text = text;
            GroupKey = groupKey;
            _groupFormat = groupFormat;
            TimeLeft = lifetime;
        }

        public void Merge(float lifetime)
        {
            Count++;
            TimeLeft = lifetime;
            SinceLastMerge = 0f;
        }

        public void Advance(float seconds)
        {
            TimeLeft -= seconds;
            SinceLastMerge += seconds;
        }
    }
}
