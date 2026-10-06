using System;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HeroUiControllerLifecycleTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SceneTeardown_UnsubscribesBeforeEntityLateDispose(bool destroyViewFirst)
        {
            HeroUi prefab = AssetDatabase.LoadAssetAtPath<HeroUi>(
                "Assets/Prefabs/Ui/Heroes/HeroUi.prefab");
            Assert.That(prefab, Is.Not.Null);
            HeroUi ui = Object.Instantiate(prefab);
            ui.gameObject.SetActive(true);

            try
            {
                FactionCatalog factions = AssetDatabase.LoadAssetAtPath<FactionCatalog>(
                    "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset");
                ShipType heroType = factions.Factions.SelectMany(faction => faction.Ships)
                    .First(ship => ship.Value.IsHero).Key;
                EntityLocator entities = new EntityLocator();
                BattleStateNotifier battleState = new BattleStateNotifier();
                HeroUiController controller = new HeroUiController(
                    new UiServiceStub(ui), entities, factions,
                    TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()),
                    null, null, battleState);
                Entity hero = new Entity(new LivingHealth(), entities,
                    new IEntityFacade[] { new UnitTypeFacade(UnitTypeId.Ship(heroType)) },
                    TestPlayers.Human, 42);
                DiContainer container = new DiContainer();
                container.BindInterfacesTo<HeroUiController>().FromInstance(controller);
                container.Bind<DisposableManager>().AsSingle();
                DisposableManager disposables = container.Resolve<DisposableManager>();
                DiContainer entityContainer = new DiContainer(container);
                entityContainer.BindInterfacesTo<Entity>().FromInstance(hero);
                entityContainer.Bind<DisposableManager>().AsSingle();
                DisposableManager entityDisposables = entityContainer.Resolve<DisposableManager>();
                controller.Initialize();
                hero.Initialize();
                Assert.That(ui.IsVisible, Is.True);
                controller.Tick();

                if (destroyViewFirst)
                {
                    ui.Dispose();
                    Object.DestroyImmediate(ui.gameObject);
                }

                Assert.DoesNotThrow(() =>
                {
                    if (!destroyViewFirst)
                        disposables.Dispose();
                    entityDisposables.Dispose();
                    entityDisposables.LateDispose();
                    if (destroyViewFirst)
                        disposables.Dispose();
                    disposables.LateDispose();
                });
                Assert.That(entities.Entities, Is.Empty);
                Assert.That(battleState.ObserverCount, Is.Zero);
                Assert.DoesNotThrow(() =>
                {
                    hero.Initialize();
                    controller.Tick();
                    controller.FocusHero(hero.Id);
                    hero.LateDispose();
                });
            }
            finally
            {
                if (ui != null)
                    Object.DestroyImmediate(ui.gameObject);
            }
        }

        private sealed class UiServiceStub : IUiService
        {
            private readonly HeroUi _ui;

            public UiServiceStub(HeroUi ui) => _ui = ui;

            public Transform DefaultCanvasTransform => throw new NotSupportedException();
            public Transform DynamicCanvasTransform => throw new NotSupportedException();
            public Transform PopupCanvasTransform => throw new NotSupportedException();

            public BaseUi CreateUi(UiType uiType) => _ui;

            public BaseUi CreateUi(UiType uiType, Transform parent) =>
                throw new NotSupportedException();

            public void SetHudVisible(bool isVisible) => throw new NotSupportedException();
        }

        private sealed class BattleStateNotifier : INotifier<BattleState>
        {
            public int ObserverCount { get; private set; }

            public void AddObserver(IObserver<BattleState> observer)
            {
                ObserverCount++;
                observer.UpdateState(BattleState.Running);
            }

            public void RemoveObserver(IObserver<BattleState> observer) => ObserverCount--;
        }

        private sealed class LivingHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public bool IsDestroyed => false;
            public ShipClass ShipClass => throw new NotSupportedException();
            public HardPointModel[] HardPointModels => throw new NotSupportedException();
            public float Hull => throw new NotSupportedException();
            public float HullPercentage => throw new NotSupportedException();
            public float Shields => throw new NotSupportedException();
            public float ShieldPercentage => throw new NotSupportedException();
            public bool IsLostShieldGenerator => throw new NotSupportedException();
            public bool HasUnits => throw new NotSupportedException();
            public bool HasLiveHardPoints => throw new NotSupportedException();
            public bool HasShields => throw new NotSupportedException();
            public PlayerId Owner => TestPlayers.Human;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) =>
                throw new NotSupportedException();
        }
    }
}
