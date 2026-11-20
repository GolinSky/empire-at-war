using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemySquadronCommander
    {
        /// <summary>Takes over orders for a station-launched AI squadron until it is released.</summary>
        void SetStationDefense(UnityEngine.Vector3? position);

        void Command(ISquadron squadron, SquadronType squadronType);

        /// <summary>Wings sent to the own-side outer zone at the opening: 2 when fighter wings are cheap, else 1.</summary>
        void SetOuterWingCount(int count);

        /// <summary>From level 2 the patrol unit is a fighter or interceptor wing instead of a corvette.</summary>
        void SetWingPatrol(bool enabled);

        /// <summary>A patrol wing that met hostile ships and holds a safe point behind them: ships spawn there first.</summary>
        bool TryGetSpawnBeacon(out UnityEngine.Vector3 position);

        /// <summary>A patrol wing is wanted but none is free: production buys one outside the wing cap.</summary>
        bool NeedsPatrolWing { get; }
    }
}
