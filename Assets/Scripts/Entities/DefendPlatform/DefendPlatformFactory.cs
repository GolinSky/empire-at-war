using EmpireAtWar.Models.Factions;
using UnityEngine;
using EmpireAtWar.Models.Players;
using Zenject;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatformFactory:PlaceholderFactory<PlayerId,DefendPlatformType,Vector3,DefendPlatform>{}
}
