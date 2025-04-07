using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Controllers.DefendPlatform;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ViewImpl;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Components.ViewComponents;
using UnityEngine;
using Zenject;

namespace EmpireAtWar
{
    public class ViewInstaller : MonoInstaller
    {
        [SerializeField] private View view;
        private IRepository repository;
        private PlayerType playerType;
        private DefendPlatformType miningFacilityType;
        private Vector3 startPosition;

        [Inject]
        public void Constructor(
            DefendPlatformType miningFacilityType,
            IRepository repository,
            PlayerType playerType,
            Vector3 startPosition)
        {
            this.miningFacilityType = miningFacilityType;
            this.repository = repository;
            this.playerType = playerType;
            this.startPosition = startPosition;
        }

        private void OnValidate()
        {
            view = GetComponent<View>();
        }

        public override void InstallBindings()
        {
            Container
                .Bind<Transform>()
                .WithId(EntityBindType.ViewTransform)
                .FromMethod(() => transform)
                .NonLazy();

            Container.BindEntity(startPosition);
            Container.BindEntity(playerType);

            Container
                .BindInterfacesExt<HealthComponent>()
                .BindInterfacesExt<SimpleMoveComponent>()
                .BindInterfacesExt<RadarComponent>()
                .BindInterfacesExt<WeaponComponent>();

            switch (playerType)
            {
                case PlayerType.Player:
                    Container.BindInterfacesExt<SelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindInterfacesExt<EnemySelectionComponent>();
                    break;
            }

            Container
                .BindModel<DefendPlatformModel>(repository)
                .BindInterfacesExt<DefendPlatformController>();

            Container
                .BindInterfacesAndSelfTo(view.GetType())
                .FromInstance(view)
                .AsSingle();

            foreach (ViewComponent component in view.ViewComponents)
            {
                Container.Inject(component);
                Container
                    .BindInterfacesTo(component.GetType())
                    .FromComponentOn(component.gameObject)
                    .AsSingle();
            }
        }
    }
}