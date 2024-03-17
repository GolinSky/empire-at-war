using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.MiningFacility;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Views.MiningFacility;
using LightWeightFramework.Components;
using LightWeightFramework.Components.Repository;
using LightWeightFramework.Components.ViewComponents;
using LightWeightFramework.Controller;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext
{
    public class MiningFacilityInstaller : Installer
    {
        private readonly PlayerType playerType;
        private readonly MiningFacilityType miningFacilityType;
        private readonly Vector3 startPosition;

        [Inject]
        private IRepository Repository { get; }

        public MiningFacilityInstaller(
            PlayerType playerType,
            MiningFacilityType miningFacilityType,
            Vector3 startPosition)
        {
            this.playerType = playerType;
            this.miningFacilityType = miningFacilityType;
            this.startPosition = startPosition;
        }

        public override void InstallBindings()
        {
            Container
                .BindInstance(startPosition)
                .AsSingle();

            Container
                .BindInstance(playerType)
                .AsSingle();


            Container
                .BindSingle<HealthComponent>()
                .BindSingle<MoveComponent>()
                .BindSingle<RadarComponent>();

            switch (playerType)
            {
                case PlayerType.Player:
                    Container.BindSingle<SelectionComponent>();
                    break;
                case PlayerType.Opponent:
                    Container.BindSingle<EnemySelectionComponent>();
                    break;
            
            }
            
            
            BindEntityFromPrefab<MiningFacilityController, MiningFacilityView, MiningFacilityModel>(Container);
        }
        
        public  void BindEntityFromPrefab<TController, TView, TModel>( DiContainer container)
            where TController : Controller<TModel>
            where TView : Component, IView
            where TModel : Model
        {
            IRepository repository = container.Resolve<IRepository>();
            
            container
                .BindInterfacesAndSelfTo<TModel>()
                .FromNewScriptableObject(repository.Load<TModel>((typeof(TModel).Name)))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    Model model = (Model)o;
                    foreach (IModel currentModel in model.CurrentModels)
                    {
                        context.Container.Inject(currentModel);
                    }
                })
                .NonLazy();

            container
                .BindInterfacesAndSelfTo<TController>()
                .AsSingle();
            
            container
                .BindInterfacesAndSelfTo<TView>()
                .FromComponentInNewPrefab(repository.Load<GameObject>($"{typeof(TView).Name}"))
                .AsSingle()
                .OnInstantiated((context, o) =>
                {
                    TView view = (TView)o;
                    foreach (ViewComponent component in view.ViewComponents)
                    {
                        context.Container.Inject(component);
                        context.Container.BindInterfacesTo(component.GetType()).FromComponentOn(component.gameObject).AsSingle();
                    }
                } );
        }
    }
}