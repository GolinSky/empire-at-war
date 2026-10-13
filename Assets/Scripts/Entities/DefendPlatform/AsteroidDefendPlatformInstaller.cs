using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar
{
    public sealed class AsteroidDefendPlatformInstaller : DefendPlatformInstaller
    {
        public AsteroidDefendPlatformInstaller(DefendPlatform view, DefendPlatformData data,
            ILocalPlayer localPlayer, PlayerId owner, long entityId, Vector3 position, DefendPlatformType type)
            : base(view, data, localPlayer, owner, entityId, position, type) { }
    }
}
