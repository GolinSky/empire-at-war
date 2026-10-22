namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Copies of <c>TargetSelectionCapture</c> slot flags and <c>TargetPreference</c> values.</summary>
    internal static class TargetFilterFlags
    {
        public const byte SLOT_VALID = 1;
        public const byte SLOT_STRIKECRAFT = 2;

        public const int PREFERENCE_ANY = 0;
        public const int PREFERENCE_STRIKECRAFT = 1;
        public const int PREFERENCE_SHIPS = 2;

        /// <summary>Copy of <c>WeaponTargetSelectionJob.IsPreferred</c>.</summary>
        public static bool IsPreferred(int preference, byte slotFlags) =>
            preference == PREFERENCE_ANY ||
            preference == PREFERENCE_STRIKECRAFT == ((slotFlags & SLOT_STRIKECRAFT) != 0);
    }
}
