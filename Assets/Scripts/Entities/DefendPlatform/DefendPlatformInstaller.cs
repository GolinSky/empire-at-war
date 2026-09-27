using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformData>
    {
        private PlayerType _playerType;
        private DefendPlatformType _defendPlatformType;

        [Inject]
        public void Construct(DefendPlatformType defendPlatformType, PlayerType playerType)
        {
            _defendPlatformType = defendPlatformType;
            _playerType = playerType;
        }

        protected override void InstallFeatures(DefendPlatformData data)
        {
            Container.BindEntityExt(_playerType);
            Container.BindEntityExt(_defendPlatformType);
            Container.BindInterfacesTo<EntityComponentData>().FromInstance(data.ComponentData);

            Container
                .BindSelectionFeature(SelectionType.DefendPlatform)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindStationaryCombatFeature()
                .BindFogOfWarFeature(_playerType);
        }
    }
}
