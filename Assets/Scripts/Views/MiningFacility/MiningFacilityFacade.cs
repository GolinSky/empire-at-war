using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.MiningFacility
{
    public class MiningFacilityFacade : PlaceholderFactory<PlayerType,MiningFacilityType,Vector3,MiningFacilityView> {}
}