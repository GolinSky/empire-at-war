using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class AsteroidDefendPlatformFactory
        : PlaceholderFactory<PlayerType, DefendPlatformType, Vector3, DefendPlatform>
    {
    }
}
