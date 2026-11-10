using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>
    /// Text cards for the local player: at most <see cref="MAX_VISIBLE"/> shown, the rest wait by priority.
    /// A more important card replaces a visible progress or feedback card. Repeats of a grouped card update it
    /// in place for up to <see cref="MAX_AGE_FACTOR"/> lifetimes; a dismissed group stays quiet for a while.
    /// </summary>
    public sealed class BattleNotificationFeed : IBattleNotifications, IBattleNotificationFeedObserver
    {
        public const int MAX_VISIBLE = 3;
        public const int MAX_PENDING = 8;
        public const float MAX_AGE_FACTOR = 2f;
        public const float DISMISS_MUTE_SECONDS = 15f;
        private const float OUTCOME_SECONDS = 12f;
        private const float URGENT_SECONDS = 8f;
        private const float TACTICAL_SECONDS = 6f;
        private const float FEEDBACK_SECONDS = 3f;
        private const float PROGRESS_SECONDS = 4f;

        private readonly List<BattleNotification> _visible = new List<BattleNotification>();
        private readonly List<BattleNotification> _pending = new List<BattleNotification>();
        private readonly Dictionary<string, float> _mutedUntil = new Dictionary<string, float>();
        private int _nextId;
        private float _clock;
        private bool _isAccepting;

        public event Action<BattleNotification> Shown;
        public event Action<BattleNotification> Updated;
        public event Action<BattleNotification> Hidden;

        public IReadOnlyList<BattleNotification> Visible => _visible;
        public IReadOnlyList<BattleNotification> Pending => _pending;

        public void Post(BattleNotificationPriority priority, string text, string groupKey = null, string groupFormat = null,
            string iconKey = null) =>
            Add(priority, text, groupKey, groupFormat, true, iconKey);

        public void Hold(BattleNotificationPriority priority, string text, string key, string iconKey = null) =>
            Add(priority, text, key, null, false, iconKey);

        /// <summary>
        /// Gameplay cards are accepted only while the battle runs, so loading and save restore stay silent.
        /// Outcome cards are always accepted.
        /// </summary>
        public void SetAccepting(bool isAccepting) => _isAccepting = isAccepting;

        /// <summary>Stops gameplay cards and clears every card except the outcome.</summary>
        public void EndBattle()
        {
            _isAccepting = false;
            _pending.RemoveAll(notification => notification.Priority != BattleNotificationPriority.Outcome);
            for (int i = _visible.Count - 1; i >= 0; i--)
                if (_visible[i].Priority != BattleNotificationPriority.Outcome) Hide(i);
            ShowPending();
        }

        public void Dismiss(int id)
        {
            int index = _visible.FindIndex(notification => notification.Id == id);
            if (index < 0) return;
            string groupKey = _visible[index].GroupKey;
            if (groupKey != null) _mutedUntil[groupKey] = _clock + DISMISS_MUTE_SECONDS;
            Hide(index);
            ShowPending();
        }

        public void Advance(float seconds)
        {
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            _clock += seconds;
            for (int i = _visible.Count - 1; i >= 0; i--)
            {
                _visible[i].Advance(seconds);
                if (_visible[i].TimeLeft <= 0f) Hide(i);
            }
            ShowPending();
        }

        private void Add(BattleNotificationPriority priority, string text, string groupKey, string groupFormat,
            bool counts, string iconKey)
        {
            if (!_isAccepting && priority != BattleNotificationPriority.Outcome) return;
            if (groupKey != null && (IsMuted(groupKey) || TryRepeat(groupKey, counts, iconKey))) return;
            float lifetime = Lifetime(priority);
            var notification = new BattleNotification(++_nextId, priority, text, groupKey, groupFormat, lifetime,
                lifetime * MAX_AGE_FACTOR, iconKey);
            if (_visible.Count < MAX_VISIBLE)
            {
                Show(notification);
                return;
            }
            int replaceable = LeastImportantReplaceable();
            if (replaceable >= 0 && _visible[replaceable].Priority > priority)
            {
                Hide(replaceable);
                Show(notification);
                return;
            }
            Enqueue(notification);
        }

        private bool IsMuted(string groupKey)
        {
            if (!_mutedUntil.TryGetValue(groupKey, out float until)) return false;
            if (_clock < until) return true;
            _mutedUntil.Remove(groupKey);
            return false;
        }

        private bool TryRepeat(string groupKey, bool counts, string iconKey)
        {
            foreach (BattleNotification notification in _visible)
            {
                if (notification.GroupKey != groupKey) continue;
                notification.Repeat(counts, iconKey);
                if (counts) Updated?.Invoke(notification);
                return true;
            }
            foreach (BattleNotification notification in _pending)
            {
                if (notification.GroupKey != groupKey) continue;
                notification.Repeat(counts, iconKey);
                return true;
            }
            return false;
        }

        private int LeastImportantReplaceable()
        {
            int result = -1;
            for (int i = 0; i < _visible.Count; i++)
            {
                BattleNotificationPriority priority = _visible[i].Priority;
                if (priority != BattleNotificationPriority.Progress && priority != BattleNotificationPriority.Feedback)
                    continue;
                if (result < 0 || priority > _visible[result].Priority) result = i;
            }
            return result;
        }

        private void Enqueue(BattleNotification notification)
        {
            int index = _pending.FindIndex(waiting => waiting.Priority > notification.Priority);
            _pending.Insert(index < 0 ? _pending.Count : index, notification);
            if (_pending.Count > MAX_PENDING) _pending.RemoveAt(_pending.Count - 1);
        }

        private void ShowPending()
        {
            while (_visible.Count < MAX_VISIBLE && _pending.Count > 0)
            {
                BattleNotification next = _pending[0];
                _pending.RemoveAt(0);
                Show(next);
            }
        }

        private void Show(BattleNotification notification)
        {
            _visible.Add(notification);
            Shown?.Invoke(notification);
        }

        private void Hide(int index)
        {
            BattleNotification notification = _visible[index];
            _visible.RemoveAt(index);
            Hidden?.Invoke(notification);
        }

        private static float Lifetime(BattleNotificationPriority priority)
        {
            switch (priority)
            {
                case BattleNotificationPriority.Outcome: return OUTCOME_SECONDS;
                case BattleNotificationPriority.Urgent: return URGENT_SECONDS;
                case BattleNotificationPriority.Tactical: return TACTICAL_SECONDS;
                case BattleNotificationPriority.Feedback: return FEEDBACK_SECONDS;
                case BattleNotificationPriority.Progress: return PROGRESS_SECONDS;
                default: throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown notification priority.");
            }
        }
    }
}
