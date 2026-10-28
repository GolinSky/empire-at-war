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
using VContainer;
using EmpireAtWar.Lifecycle;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
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
                    TestPlayers.Human, 42, System.Array.Empty<IPooledEntityEvents>(),
                    System.Array.Empty<EmpireAtWar.Components.Hangar.IHangarLaunchEvents>());
                ContainerBuilder builder = new ContainerBuilder();
                builder.RegisterInstance(controller).AsImplementedInterfaces();
                builder.RegisterLifecycle();
                using IObjectResolver container = builder.Build();
                using IScopedObjectResolver entityContainer = container.CreateScope(childBuilder =>
                {
                    childBuilder.RegisterInstance(hero).AsImplementedInterfaces();
                    childBuilder.RegisterLifecycle();
                });
                OrderedLifecycleCoordinator disposables = container.Resolve<OrderedLifecycleCoordinator>();
                OrderedLifecycleCoordinator entityDisposables = entityContainer.Resolve<OrderedLifecycleCoordinator>();
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
                    if (destroyViewFirst)
                        disposables.Dispose();
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

            public TUi CreateUi<TUi>() where TUi : BaseUi => (TUi)(BaseUi)_ui;

            public void SetContentLayout(bool isFaction, bool isGroup) { }
            public void SetContentVisible(bool isVisible) { }

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
