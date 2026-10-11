using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Extensions;
using EmpireAtWar.Services.Selection;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformData>
    {
        private PlayerId _owner;
        private DefendPlatformType _defendPlatformType;

        private bool _isHiddenByLocalFog;

        protected override string DataPath => _defendPlatformType switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformData",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformData",
            _ => base.DataPath,
        };
        protected override string PrefabPath => _defendPlatformType switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformView",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformView",
            _ => base.PrefabPath,
        };

        // Golan III and Empress act as a station for production: they keep buying open and open the faction UI.
        private bool IsProductionBase =>
            _defendPlatformType == DefendPlatformType.GolanIII || _defendPlatformType == DefendPlatformType.Empress;

        [Inject]
        public void Construct(ILocalPlayer localPlayer, DefendPlatformType defendPlatformType, PlayerId owner)
        {
            _isHiddenByLocalFog = !localPlayer.IsFriendly(owner);
            _defendPlatformType = defendPlatformType;
            _owner = owner;
        }

        protected override void InstallFeatures(DefendPlatformData data)
        {
            Container.BindEntityExt(_owner);
            Container.BindEntityExt(_defendPlatformType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);
            Container.BindInterfacesExt<DefendPlatformTooltipFacade>();
            if (IsProductionBase)
            {
                Container.BindInterfacesExt<ProductionBaseFacade>();
            }

            Container
                .BindSelectionFeature(
                    SelectionType.DefendPlatform,
                    IsProductionBase ? SelectionType.Base : SelectionType.DefendPlatform)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindStationaryCombatFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog)
                .BindFogVisionFeature()
                .BindSpawnBlockerFeature();
        }
    }
}
