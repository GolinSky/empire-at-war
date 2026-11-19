using System;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public sealed class StructureStatusIconModel
    {
        public const float WORLD_DISPLAY_SECONDS = 2f;
        public const float MINIMAP_HOLD_SECONDS = 3f;
        public const float FADE_IN_SECONDS = 0.2f;
        public const float FADE_OUT_SECONDS = 0.3f;
        public const float MINIMAP_TOTAL_SECONDS = FADE_IN_SECONDS + MINIMAP_HOLD_SECONDS + FADE_OUT_SECONDS;

        private StructureStatusIconRecord[] _records = Array.Empty<StructureStatusIconRecord>();
        private int[] _activeSources = Array.Empty<int>();
        private int[] _activePositions = Array.Empty<int>();

        public int ActiveCount { get; private set; }

        public void Reset(int sourceCount)
        {
            _records = new StructureStatusIconRecord[sourceCount];
            _activeSources = new int[sourceCount];
            _activePositions = new int[sourceCount];
            for (int i = 0; i < sourceCount; i++) _activePositions[i] = -1;
            ActiveCount = 0;
        }

        public StructureStatusIconRecord Get(int sourceIndex) => _records[sourceIndex];

        // Advance/Clear swap-remove expired entries; iterate backwards when advancing a frame.
        public int GetActiveSource(int activeIndex) => _activeSources[activeIndex];

        public void Show(int sourceIndex, StructureStatusIconState state, bool showBuildReminder = false)
        {
            _records[sourceIndex] = new StructureStatusIconRecord
            {
                State = state,
                ShowBuildReminder = showBuildReminder,
                WorldActive = true,
                MinimapActive = true,
                WorldSecondsLeft = WORLD_DISPLAY_SECONDS
            };
            Activate(sourceIndex);
        }

        public void RestoreBuildAvailable(int sourceIndex)
        {
            _records[sourceIndex] = new StructureStatusIconRecord
            {
                State = StructureStatusIconState.BuildAvailable,
                WorldActive = true
            };
            Activate(sourceIndex);
        }

        public void Dismiss(int sourceIndex)
        {
            ref StructureStatusIconRecord record = ref _records[sourceIndex];
            if (record.IsDismissing) return;
            record.WorldDismissAlpha = record.WorldAlpha;
            record.MinimapDismissAlpha = record.MinimapAlpha;
            record.IsDismissing = true;
            record.WorldSecondsLeft = FADE_OUT_SECONDS;
            record.MinimapSeconds = 0f;
            record.WorldActive &= record.WorldDismissAlpha > 0f;
            record.MinimapActive &= record.MinimapDismissAlpha > 0f;
            if (!record.WorldActive && !record.MinimapActive) Clear(sourceIndex);
        }

        public void Clear(int sourceIndex)
        {
            _records[sourceIndex] = default;
            int position = _activePositions[sourceIndex];
            if (position < 0) return;
            int lastSource = _activeSources[--ActiveCount];
            _activeSources[position] = lastSource;
            _activePositions[lastSource] = position;
            _activePositions[sourceIndex] = -1;
        }

        public void Advance(int sourceIndex, float seconds, bool worldDisplayed, bool minimapDisplayed)
        {
            ref StructureStatusIconRecord record = ref _records[sourceIndex];
            if (record.WorldActive && worldDisplayed)
            {
                record.WorldFadeInSeconds = Math.Min(FADE_IN_SECONDS, record.WorldFadeInSeconds + seconds);
                if (!record.IsPersistent || record.IsDismissing)
                {
                    record.WorldSecondsLeft = Math.Max(0f, record.WorldSecondsLeft - seconds);
                    record.WorldActive = record.WorldSecondsLeft > 0f;
                    if (!record.WorldActive && record.ShowBuildReminder && !record.IsDismissing)
                    {
                        record.State = StructureStatusIconState.BuildAvailable;
                        record.WorldActive = true;
                        record.WorldFadeInSeconds = 0f;
                        record.ShowBuildReminder = false;
                    }
                }
            }
            if (record.MinimapActive && minimapDisplayed)
            {
                record.MinimapSeconds += seconds;
                record.MinimapActive = record.MinimapSeconds <
                    (record.IsDismissing ? FADE_OUT_SECONDS : MINIMAP_TOTAL_SECONDS);
            }
            if (!record.WorldActive && !record.MinimapActive) Clear(sourceIndex);
        }

        private void Activate(int sourceIndex)
        {
            if (_activePositions[sourceIndex] >= 0) return;
            _activePositions[sourceIndex] = ActiveCount;
            _activeSources[ActiveCount++] = sourceIndex;
        }
    }
}
