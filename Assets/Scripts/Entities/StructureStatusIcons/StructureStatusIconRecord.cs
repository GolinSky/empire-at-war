namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public struct StructureStatusIconRecord
    {
        public StructureStatusIconKind Kind { get; internal set; }
        public StructureStatusIconState State { get; internal set; }
        public bool WorldActive { get; internal set; }
        public bool MinimapActive { get; internal set; }
        public float WorldSecondsLeft { get; internal set; }
        internal float MinimapSeconds { get; set; }

        public float MinimapAlpha => !MinimapActive ? 0f :
            MinimapSeconds <= StructureStatusIconModel.MINIMAP_HOLD_SECONDS ? 1f :
            1f - (MinimapSeconds - StructureStatusIconModel.MINIMAP_HOLD_SECONDS) /
            StructureStatusIconModel.MINIMAP_FADE_SECONDS;

        internal bool IsPersistent => State == StructureStatusIconState.Captured &&
                                      Kind != StructureStatusIconKind.Relay;
    }
}
