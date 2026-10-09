using EmpireAtWar.Entities.Units;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.EntityFacades.Health;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Squadrons.EntityFacades;
using EmpireAtWar.Extensions;
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
        private bool _isLocal;

        protected override string DataPath => _squadronType + base.DataPath;
        protected override string PrefabPath => _squadronType + base.PrefabPath;

        [Inject]
        public void Construct(ILocalPlayer localPlayer, PlayerId owner, SquadronType squadronType, Quaternion startRotation)
        {
            _isHiddenByLocalFog = !localPlayer.IsFriendly(owner);
            _isLocal = localPlayer.IsLocal(owner);
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
            Container.BindInterfacesExt<EmpireAtWar.Entities.Ship.EntityFacades.Combat.CombatModifiersFacade>();

            Container.BindInitializableExecutionOrder<SquadronHealthComponent>(-100);
            Container.BindInitializableExecutionOrder<SquadronFlightComponent>(-90);
            Container.Bind<SquadronHealthModel>().AsSingle();
            if (data.SeekerWarheadRange > 0f)
            {
                Container.BindInterfacesAndSelfTo<SeekerWarheadCountermeasure>().AsSingle();
            }
            Container.Bind<SquadronFlightModel>().AsSingle();
            Container.Bind<SFoilsModel>().AsSingle();
            if (System.Array.IndexOf(data.Abilities, EmpireAtWar.Services.ShipAbilities.ShipAbilityId.LockSFoils) >= 0)
            {
                Container.BindInterfacesAndSelfTo<EmpireAtWar.ViewComponents.Squadrons.SFoilsView>().FromComponentInHierarchy().AsCached();
                Container.BindInterfacesExt<SFoilsFacade>();
            }
            Container.Bind<UnitOrderModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<SquadronPilot>().AsSingle();
            Container.Bind<SquadronTargetSelector>().AsSingle();

            Container.BindInterfacesAndSelfTo<SquadronHealthComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronFlightComponent>().FromComponentInHierarchy().AsCached();
            Container.BindInterfacesAndSelfTo<SquadronIconComponent>().FromComponentInHierarchy().AsCached();
            // Voice lines only play for the local player's own squadrons.
            if (_isLocal)
                Container.BindInterfacesAndSelfTo<AudioDialogShipComponent>().FromComponentInHierarchy().AsCached();

            Container.BindInterfacesExt<SquadronOrderFacade>();
            Container.BindInterfacesExt<SquadronAbilityFacade>();
            Container.BindInterfacesExt<HealthFacade>();
            Container.BindInterfacesExt<SquadronTooltipFacade>();
            Container.BindInterfacesAndSelfTo<UnitTypeFacade>().AsSingle().WithArguments(UnitTypeId.Squadron(_squadronType));
        }
    }
}
