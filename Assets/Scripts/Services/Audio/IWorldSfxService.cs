using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    public interface IWorldSfxService
    {
        bool TryPlayOneShot(PlayerId owner, SfxProfile sound, Vector3 position);
        void StopOneShots(SfxProfile sound);
    }
}
