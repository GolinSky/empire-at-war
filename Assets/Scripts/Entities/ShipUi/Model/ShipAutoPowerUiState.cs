namespace EmpireAtWar.Models.ShipUi
{
    public readonly struct ShipAutoPowerUiState
    {
        public bool Visible { get; }
        public bool Enabled { get; }
        public bool Mixed { get; }
        public bool GlobalEnabled { get; }
        public bool Paused { get; }

        public ShipAutoPowerUiState(int eligibleCount, int enabledCount, bool globalEnabled,
            bool individuallySelected)
        {
            Visible = eligibleCount > 0;
            Enabled = Visible && enabledCount == eligibleCount;
            Mixed = enabledCount > 0 && enabledCount < eligibleCount;
            GlobalEnabled = globalEnabled;
            Paused = individuallySelected && Enabled && globalEnabled;
        }

        public string Description => !GlobalEnabled
            ? "Disabled in Settings. Each ship's automatic-use preference is retained."
            : Paused
                ? "Automatic use paused while individually selected. The current ability continues normally."
                : Mixed
                    ? "Mixed automatic-use preferences. Click to enable automatic power abilities for all eligible ships in this group."
                    : Enabled
                        ? "Automatic power abilities enabled. Click to disable future automatic use."
                        : "Automatic power abilities disabled. Click to enable future automatic use.";
    }
}
