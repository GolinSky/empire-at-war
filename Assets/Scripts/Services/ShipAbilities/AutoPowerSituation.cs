namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>One caster's combat state for a decision; says which automatic roles are wanted now.</summary>
    public readonly struct AutoPowerSituation
    {
        // Survival first, then the shield/weapon power choice, then attacks, then travel.
        public static readonly AutoPowerRole[] Priority =
        {
            AutoPowerRole.Cloak, AutoPowerRole.Defensive, AutoPowerRole.Shields, AutoPowerRole.Weapons,
            AutoPowerRole.Target, AutoPowerRole.Engage, AutoPowerRole.Travel
        };

        public AutoPowerRole Power { get; }
        public bool HostileNearby { get; }
        public bool Engaged { get; }
        public bool Endangered { get; }
        public bool Cornered { get; }
        public bool Travelling { get; }

        public AutoPowerSituation(AutoPowerRole power, bool hostileNearby, bool engaged, bool endangered,
            bool cornered, bool travelling)
        {
            Power = power;
            HostileNearby = hostileNearby;
            Engaged = engaged;
            Endangered = endangered;
            Cornered = cornered;
            Travelling = travelling;
        }

        public bool Wants(AutoPowerRole role)
        {
            switch (role)
            {
                case AutoPowerRole.Cloak: return Cornered && HostileNearby;
                case AutoPowerRole.Defensive: return Endangered && HostileNearby;
                case AutoPowerRole.Shields:
                case AutoPowerRole.Weapons: return Power == role;
                case AutoPowerRole.Target: return HostileNearby;
                case AutoPowerRole.Engage: return Engaged;
                case AutoPowerRole.Travel: return Travelling && !HostileNearby;
                default: return false;
            }
        }
    }
}
