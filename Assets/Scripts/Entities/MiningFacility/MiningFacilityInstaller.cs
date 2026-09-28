using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Selection;
using Zenject;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicEntityInstaller<MiningFacilityEntity, MiningFacilityData>
    {
        private PlayerId _owner;
        private bool _isHiddenByLocalFog;
        private MiningFacilityType _miningFacilityType;

        [Inject]
        public void Construct(PlayerId owner, MiningFacilityType miningFacilityType, ILocalPlayer localPlayer)
        {
            _isHiddenByLocalFog = !localPlayer.IsFriendly(owner);
            _owner = owner;
            _miningFacilityType = miningFacilityType;
        }

        protected override void InstallFeatures(MiningFacilityData data)
        {
            Container.BindEntityExt(_owner);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);

            Container
                .BindSelectionFeature(SelectionType.MiningFacility)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindCombatModifiersFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog);
        }
    }
}
