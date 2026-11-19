namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>What a force should do about one zone. There is no bare "no": a weak force assembles instead.</summary>
    public enum EnemyZoneVerdict
    {
        /// <summary>The force beats the known defenders: go now.</summary>
        Go = 0,
        /// <summary>The force is too small: gather at the staging point until it is enough.</summary>
        Assemble = 1,
        /// <summary>Hard reason only (inside the hostile base before the assault gate, unreachable).</summary>
        Deny = 2
    }
}
