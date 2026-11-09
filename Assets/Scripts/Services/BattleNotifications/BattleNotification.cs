using System;

namespace EmpireAtWar.Services.BattleNotifications
{
    public sealed class BattleNotification
    {
        private readonly string _text;
        private readonly string _groupFormat;
        private readonly float _lifetime;
        private readonly float _maxAge;

        public int Id { get; }
        public BattleNotificationPriority Priority { get; }
        public string GroupKey { get; }
        public int Count { get; private set; } = 1;
        public float TimeLeft { get; private set; }
        public float Age { get; private set; }

        public string Text => Count == 1 ? _text
            : _groupFormat != null ? string.Format(_groupFormat, Count) : $"{_text} ×{Count}";

        /// <param name="maxAge">Repeated events stop extending the card past this age, so it cannot stay forever.</param>
        public BattleNotification(int id, BattleNotificationPriority priority, string text, string groupKey,
            string groupFormat, float lifetime, float maxAge)
        {
            Id = id;
            Priority = priority;
            _text = text;
            GroupKey = groupKey;
            _groupFormat = groupFormat;
            _lifetime = lifetime;
            _maxAge = maxAge;
            TimeLeft = lifetime;
        }

        /// <param name="counts">
        /// True adds one to the count, extending the card only up to its max age. False keeps the card on screen
        /// for one more lifetime, so a held card stays while its condition lasts.
        /// </param>
        public void Repeat(bool counts)
        {
            if (!counts)
            {
                TimeLeft = _lifetime;
                return;
            }
            Count++;
            TimeLeft = Math.Max(TimeLeft, Math.Min(_lifetime, _maxAge - Age));
        }

        public void Advance(float seconds)
        {
            TimeLeft -= seconds;
            Age += seconds;
        }
    }
}
