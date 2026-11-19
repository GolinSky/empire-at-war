using System;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public struct StructureStatusIconRecord
    {
        public StructureStatusIconState State { get; internal set; }
        // The world reminder can replace capture before the minimap confirmation expires.
        public StructureStatusIconState MinimapState => IsPersistent ? StructureStatusIconState.Captured : State;
        public bool WorldActive { get; internal set; }
        public bool MinimapActive { get; internal set; }
        public float WorldSecondsLeft { get; internal set; }
        internal float WorldFadeInSeconds { get; set; }
        internal float MinimapSeconds { get; set; }
        internal bool IsDismissing { get; set; }
        internal bool ShowBuildReminder { get; set; }
        internal float WorldDismissAlpha { get; set; }
        internal float MinimapDismissAlpha { get; set; }

        public float WorldAlpha => !WorldActive ? 0f : IsDismissing
            ? WorldDismissAlpha * Math.Min(1f, WorldSecondsLeft / StructureStatusIconModel.FADE_OUT_SECONDS)
            : Math.Min(WorldFadeInSeconds / StructureStatusIconModel.FADE_IN_SECONDS,
                IsPersistent ? 1f : Math.Min(1f, WorldSecondsLeft / StructureStatusIconModel.FADE_OUT_SECONDS));

        public float MinimapAlpha => !MinimapActive ? 0f :
            IsDismissing ? MinimapDismissAlpha * Math.Max(0f,
                1f - MinimapSeconds / StructureStatusIconModel.FADE_OUT_SECONDS) :
            Math.Min(1f, Math.Min(MinimapSeconds / StructureStatusIconModel.FADE_IN_SECONDS,
                (StructureStatusIconModel.MINIMAP_TOTAL_SECONDS - MinimapSeconds) /
                StructureStatusIconModel.FADE_OUT_SECONDS));

        internal bool IsPersistent => State == StructureStatusIconState.BuildAvailable;
    }
}
