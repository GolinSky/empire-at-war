using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.DefendPlatform
{
    public class DefendPlatformFacade:PlaceholderFactory<PlayerType,DefendPlatformType,Vector3,DefendPlatformView>{}
}