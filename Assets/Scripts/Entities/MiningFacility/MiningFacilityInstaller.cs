using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using Zenject;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public class MiningFacilityInstaller : DynamicEntityInstaller<MiningFacilityEntity, MiningFacilityData>
    {
        private PlayerType _playerType;
        private MiningFacilityType _miningFacilityType;

        [Inject]
        public void Construct(PlayerType playerType, MiningFacilityType miningFacilityType)
        {
            _playerType = playerType;
            _miningFacilityType = miningFacilityType;
        }

        protected override void InstallFeatures(MiningFacilityData data)
        {
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_miningFacilityType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);

            Container
                .BindSelectionFeature(SelectionType.MiningFacility)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindCombatModifiersFeature()
                .BindFogOfWarFeature(_playerType);
        }
    }
}
