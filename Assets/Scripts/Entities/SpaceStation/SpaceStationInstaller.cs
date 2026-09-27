using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using Zenject;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.SpaceStation
{
    public class SpaceStationInstaller : DynamicEntityInstaller<SpaceStationEntity, SpaceStationData>
    {
        private FactionType _factionType;
        private PlayerType _playerType;

        protected override string PrefabPath => _factionType + base.PrefabPath;

        [Inject]
        public void Construct(FactionType factionType, PlayerType playerType)
        {
            _factionType = factionType;
            _playerType = playerType;
        }

        protected override void InstallFeatures(SpaceStationData data)
        {
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_factionType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);

            Container
                .BindSelectionFeature(SelectionType.Base)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindStationaryCombatFeature();

            Container.Bind<IHangarData>().To<StationHangarData>().AsSingle();
            Container.Bind<HangarModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<HangarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
        }
    }
}
