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
        private MiningFacilityType _miningFacilityType;

        private bool _isHiddenByLocalFog;

        [Inject]
        public void Construct(ILocalPlayer localPlayer, PlayerId owner, MiningFacilityType miningFacilityType)
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
            Container.BindInterfacesAndSelfTo<MiningFacilityModel>().AsSingle().WithArguments(data.Income);
            Container.BindInterfacesExt<MiningFacilityTooltipFacade>();

            Container
                .BindSelectionFeature(SelectionType.MiningFacility)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindCombatModifiersFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog);
        }
    }
}
