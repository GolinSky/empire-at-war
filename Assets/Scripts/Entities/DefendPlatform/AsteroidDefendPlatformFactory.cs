using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class AsteroidDefendPlatformFactory : PlaceholderFactory<PlayerId, DefendPlatformType, Vector3, DefendPlatform>
    {
    }
}
