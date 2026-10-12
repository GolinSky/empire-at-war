using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public interface IFighterExplosionPool
    {
        /// <summary>Plays one fighter death explosion at the position.</summary>
        void Play(Vector3 position);
    }
}
