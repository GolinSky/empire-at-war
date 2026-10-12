using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Components.Squadrons.Health
{
    public interface ISquadronHealthComponent : IHealthComponent
    {
        /// <summary>Restores every fighter to full health for a reused squadron.</summary>
        void Respawn();
    }
}
