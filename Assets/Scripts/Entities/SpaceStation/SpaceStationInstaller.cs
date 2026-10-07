using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
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
        private PlayerId _owner;
        private bool _isHiddenByLocalFog;

        protected override string PrefabPath => _factionType + base.PrefabPath;

        [Inject]
        public void Construct(FactionType factionType, PlayerId owner, ILocalPlayer localPlayer)
        {
            _factionType = factionType;
            _owner = owner;
            _isHiddenByLocalFog = !localPlayer.IsFriendly(owner);
        }

        protected override void InstallFeatures(SpaceStationData data)
        {
            Container.BindEntityExt(_owner);
            Container.BindEntityExt(_factionType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);
            Container.BindInterfacesExt<SpaceStationTooltipFacade>();
            Container.BindInterfacesExt<PlayerBaseFacade>();
            Container.BindInterfacesExt<StationFootprintFacade>();

            Container
                .BindSelectionFeature(SelectionType.Base)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindStationaryCombatFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog);

            Container.Bind<IHangarData>().To<StationHangarData>().AsSingle();
            Container.Bind<HangarModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<HangarComponent>()
                .FromComponentsInHierarchy()
                .AsCached();
        }
    }
}
