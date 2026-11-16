using System;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public sealed class StructureStatusIconModel
    {
        public const float WORLD_DISPLAY_SECONDS = 2f;
        public const float MINIMAP_HOLD_SECONDS = 3f;
        public const float MINIMAP_FADE_SECONDS = 0.3f;

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

        public void Show(int sourceIndex, StructureStatusIconKind kind, StructureStatusIconState state)
        {
            _records[sourceIndex] = new StructureStatusIconRecord
            {
                Kind = kind,
                State = state,
                WorldActive = true,
                MinimapActive = true,
                WorldSecondsLeft = WORLD_DISPLAY_SECONDS
            };
            Activate(sourceIndex);
        }

        public void RestoreCaptured(int sourceIndex, StructureStatusIconKind kind)
        {
            _records[sourceIndex] = new StructureStatusIconRecord
            {
                Kind = kind,
                State = StructureStatusIconState.Captured,
                WorldActive = true,
                WorldSecondsLeft = WORLD_DISPLAY_SECONDS
            };
            Activate(sourceIndex);
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
            if (record.WorldActive && worldDisplayed && !record.IsPersistent)
            {
                record.WorldSecondsLeft = Math.Max(0f, record.WorldSecondsLeft - seconds);
                record.WorldActive = record.WorldSecondsLeft > 0f;
            }
            if (record.MinimapActive && minimapDisplayed)
            {
                record.MinimapSeconds += seconds;
                record.MinimapActive = record.MinimapSeconds < MINIMAP_HOLD_SECONDS + MINIMAP_FADE_SECONDS;
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
