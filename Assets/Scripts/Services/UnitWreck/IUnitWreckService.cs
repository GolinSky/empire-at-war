using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitWreck
{
    public interface IUnitWreckService : IService
    {
        /// <summary>
        /// Places a pooled wreck at the unit's pose. It stays invisible for <paramref name="delay"/> seconds,
        /// while the explosion still hides the swap, and returns to the pool after its lifetime.
        /// </summary>
        void Spawn(UnitWreckData data, Transform unit, PlayerId owner, float delay);
    }
}
