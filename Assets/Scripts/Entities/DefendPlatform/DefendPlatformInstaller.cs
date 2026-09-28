using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Selection;
using Zenject;

namespace EmpireAtWar
{
    public class DefendPlatformInstaller : DynamicEntityInstaller<DefendPlatform, DefendPlatformData>
    {
        private PlayerId _owner;
        private bool _isHiddenByLocalFog;
        private DefendPlatformType _defendPlatformType;

        [Inject]
        public void Construct(DefendPlatformType defendPlatformType, PlayerId owner, ILocalPlayer localPlayer)
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

            Container
                .BindSelectionFeature(SelectionType.DefendPlatform)
                .BindHealthFeature()
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindCombatModifiersFeature()
                .BindStationaryCombatFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog);
        }
    }
}
