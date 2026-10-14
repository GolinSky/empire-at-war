using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Components.AttackComponent
{
    public static class TargetPreferenceExtensions
    {
        public static bool Prefers(this TargetPreference preference, ShipClass targetClass) =>
            preference == TargetPreference.Any || preference == TargetPreference.Strikecraft == targetClass.IsStrikecraft();
    }
}
