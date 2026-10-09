using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
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

            Container
                .BindSelectionFeature(SelectionType.DefendPlatform)
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
