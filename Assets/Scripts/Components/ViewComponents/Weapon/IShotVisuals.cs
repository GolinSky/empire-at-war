using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Draws one mount's shots. Combat times and lands every shot itself; a shot this cannot draw still fires.
    /// </summary>
    public interface IShotVisuals
    {
        /// <param name="missile">The interceptable missile this shot is, or null; an intercepted shot stops drawing.</param>
        void Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset, bool isHit, IncomingMissile missile,
            float lateSeconds);

        /// <summary>The mount is gone; shots already drawn finish on their own.</summary>
        void Release();
    }
}
