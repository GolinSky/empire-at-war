using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>Advances each player's authoritative cooldown, or its client display between snapshots.</summary>
    public sealed class SuperWeaponCooldownService : ITickable
    {
        private readonly SuperWeaponModel _model;

        public SuperWeaponCooldownService(SuperWeaponModel model)
        {
            _model = model;
        }

        public void Tick() => _model.Tick(Time.deltaTime);
    }
}
