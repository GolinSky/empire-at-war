using EmpireAtWar.Entities.Units;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Extentions;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Squadrons
{
    public sealed class SquadronInstaller : DynamicEntityInstaller<Squadron, SquadronData>
    {
        private SquadronType _squadronType;
        private PlayerId _owner;
        private Quaternion _startRotation;

        private bool _isHiddenByLocalFog;

        protected override string DataPath => _squadronType + base.DataPath;
        protected override string PrefabPath => _squadronType + base.PrefabPath;

        [Inject]
        public void Construct(ILocalPlayer localPlayer, PlayerId owner, SquadronType squadronType, Quaternion startRotation)
        {
            _isHiddenByLocalFog = !localPlayer.IsFriendly(owner);
            _owner = owner;
            _squadronType = squadronType;
            _startRotation = startRotation;
        }

        protected override void InstallFeatures(SquadronData data)
        {
            Container.BindEntityExt(_owner);
            Container.BindEntityExt(_squadronType);
            Container.BindEntityExt(_startRotation);

            Container
                .BindSelectionFeature(SelectionType.Ship)
                .BindRadarFeature()
                .BindWeaponFeature()
                .BindFogOfWarFeature(_isHiddenByLocalFog);
            Container.Bind<CombatModifiers>().AsSingle();

            Container.BindInitializableExecutionOrder<SquadronHealthComponent>(-100);
            Container.BindInitializableExecutionOrder<SquadronFlightComponent>(-90);
            Container.Bind<SquadronHealthModel>().AsSingle();
            Container.Bind<SquadronFlightModel>().AsSingle();
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.Bind<SquadronPilot>().AsSingle();
            Container.Bind<SquadronTargetSelector>().AsSingle();

            Container.BindInterfacesAndSelfTo<SquadronHealthComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronFlightComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronIconComponent>().FromComponentInHierarchy().AsCached();

            Container.BindInterfacesExt<SquadronOrderFacade>();
            Container.BindInterfacesExt<HealthFacade>();
            Container.BindInterfacesExt<SquadronTooltipFacade>();
            Container.BindInterfacesAndSelfTo<UnitTypeFacade>().AsSingle().WithArguments(UnitTypeId.Squadron(_squadronType));
        }
    }
}
