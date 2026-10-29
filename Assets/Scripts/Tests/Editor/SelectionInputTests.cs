using System;
using EmpireAtWar.Tests.Editor;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Views.MiniMap;
using EmpireAtWar.Entities.MiniMap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Tests.Selection
{
    [Category("BattleInput")]
    public sealed class SelectionInputTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SelectedEntityDeath_ClearsSelectionBeforeDespawn(bool local)
        {
            FakeInputService input = new FakeInputService();
            FakeSelectionQuery query = new FakeSelectionQuery();
            EntityLocator locator = new EntityLocator();
            SelectionService service = new SelectionService(input, input, input, locator, query,
                new FakeMarqueeSelectionPresenter(), TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            FakeSelectionCommand command = new FakeSelectionCommand(SelectionType.Ship);
            FakeEntity entity = new FakeEntity(command, local ? TestPlayers.Human : TestPlayers.Enemy, 1);
            locator.AddEntity(entity);
            query.Add(Vector2.zero, new SelectionEntry(entity, command));
            service.Initialize();
            try
            {
                input.RaiseSelectionBegan(Vector2.zero);
                Assert.That(command.IsSelected, Is.True);
                Assert.That(entity.Health.DeathSubscribers, Is.EqualTo(1));

                entity.Health.Destroy();

                Assert.That(command.IsSelected, Is.False);
                Assert.That(service.PlayerSelectionContext.HasSelectable, Is.False);
                Assert.That(service.OtherSelectionContext.HasSelectable, Is.False);
                Assert.That(locator.Entities, Does.Contain(entity));
                Assert.That(entity.Health.DeathSubscribers, Is.Zero);
                locator.RemoveEntity(entity);
            }
            finally
            {
                service.LateDispose();
            }
        }

        [Test]
        public void SelectionClearedOrDisposed_RemovesDeathSubscription()
        {
            FakeInputService input = new FakeInputService();
            FakeSelectionQuery query = new FakeSelectionQuery();
            SelectionService service = new SelectionService(input, input, input, new EntityLocator(), query,
                new FakeMarqueeSelectionPresenter(), TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            FakeSelectionCommand command = new FakeSelectionCommand(SelectionType.Ship);
            FakeEntity entity = new FakeEntity(command, TestPlayers.Human, 1);
            query.Add(Vector2.zero, new SelectionEntry(entity, command));
            service.Initialize();
            try
            {
                input.RaiseSelectionBegan(Vector2.zero);
                input.RaiseSelectionBegan(Vector2.one);
                Assert.That(entity.Health.DeathSubscribers, Is.Zero);
                input.RaiseSelectionBegan(Vector2.zero);
                Assert.That(entity.Health.DeathSubscribers, Is.EqualTo(1));
            }
            finally
            {
                service.LateDispose();
            }
            Assert.That(entity.Health.DeathSubscribers, Is.Zero);
        }

        [Test]
        public void SelectionBegan_ClearsPreviousSelectionBeforeApplyingHit()
        {
            Vector2 playerPosition = new Vector2(10f, 10f);
            Vector2 opponentPosition = new Vector2(20f, 20f);
            Vector2 emptyPosition = new Vector2(30f, 30f);
            FakeInputService inputService = new FakeInputService();
            FakeSelectionQuery selectionQuery = new FakeSelectionQuery();
            FakeMarqueeSelectionPresenter marqueeSelectionPresenter =
                new FakeMarqueeSelectionPresenter();
            SelectionService selectionService = new SelectionService(
                inputService,
                inputService,
                inputService,
                new EntityLocator(),
                selectionQuery,
                marqueeSelectionPresenter,
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            FakeSelectionCommand playerCommand =
                new FakeSelectionCommand(SelectionType.Ship);
            FakeSelectionCommand opponentCommand =
                new FakeSelectionCommand(SelectionType.Ship);
            selectionQuery.Add(
                playerPosition,
                new SelectionEntry(
                    new FakeEntity(id: 1, owner: TestPlayers.Human, selectionCommand: playerCommand),
                    playerCommand));
            selectionQuery.Add(
                opponentPosition,
                new SelectionEntry(
                    new FakeEntity(id: 2, owner: TestPlayers.Enemy, selectionCommand: opponentCommand),
                    opponentCommand));

            selectionService.Initialize();
            try
            {
                inputService.RaiseSelectionBegan(playerPosition);
                inputService.RaiseSelectionBegan(opponentPosition);

                Assert.That(playerCommand.IsSelected, Is.False);
                Assert.That(opponentCommand.IsSelected, Is.True);
                Assert.That(
                    selectionService.PlayerSelectionContext.HasSelectable,
                    Is.False);
                Assert.That(
                    selectionService.OtherSelectionContext.HasSelectable,
                    Is.True);

                inputService.RaiseSelectionBegan(emptyPosition);

                Assert.That(opponentCommand.IsSelected, Is.False);
                Assert.That(
                    selectionService.OtherSelectionContext.HasSelectable,
                    Is.False);
            }
            finally
            {
                selectionService.LateDispose();
            }
        }

        [Test]
        public void RepeatedTapOnSameSpot_CyclesThroughOverlappingEntities()
        {
            Vector2 position = new Vector2(10f, 10f);
            FakeInputService inputService = new FakeInputService();
            FakeSelectionQuery selectionQuery = new FakeSelectionQuery();
            SelectionService selectionService = new SelectionService(
                inputService,
                inputService,
                inputService,
                new EntityLocator(),
                selectionQuery,
                new FakeMarqueeSelectionPresenter(),
                TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()));
            FakeSelectionCommand shipCommand = new FakeSelectionCommand(SelectionType.Ship);
            FakeSelectionCommand stationCommand = new FakeSelectionCommand(SelectionType.Base);
            selectionQuery.Add(
                position,
                new SelectionEntry(
                    new FakeEntity(id: 1, owner: TestPlayers.Human, selectionCommand: shipCommand),
                    shipCommand));
            selectionQuery.Add(
                position,
                new SelectionEntry(
                    new FakeEntity(id: 2, owner: TestPlayers.Human, selectionCommand: stationCommand),
                    stationCommand));

            selectionService.Initialize();
            try
            {
                inputService.RaiseSelectionBegan(position);
                Assert.That(shipCommand.IsSelected, Is.True);
                Assert.That(stationCommand.IsSelected, Is.False);

                inputService.RaiseSelectionBegan(position);
                Assert.That(shipCommand.IsSelected, Is.False);
                Assert.That(stationCommand.IsSelected, Is.True);

                inputService.RaiseSelectionBegan(position);
                Assert.That(shipCommand.IsSelected, Is.True);
                Assert.That(stationCommand.IsSelected, Is.False);
            }
            finally
            {
                selectionService.LateDispose();
            }
        }

        [Test]
        public void MiniMapPointerExit_KeepsMapVisible()
        {
            GameObject gameObject = new GameObject(
                "MiniMap",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(MiniMapUi));
            Image mapImage = gameObject.GetComponent<Image>();
            mapImage.color = Color.white;
            MiniMapUi miniMapUi = gameObject.GetComponent<MiniMapUi>();
            GameObject footprintObject = new GameObject("CameraFootprint", typeof(RectTransform));
            footprintObject.transform.SetParent(gameObject.transform, false);
            CameraFootprintView footprintView = footprintObject.AddComponent<CameraFootprintView>();
            GameObject obstacleObject = new GameObject("Obstacles", typeof(RectTransform));
            obstacleObject.transform.SetParent(gameObject.transform, false);
            MiniMapObstacleView obstacleView = obstacleObject.AddComponent<MiniMapObstacleView>();
            FieldInfo footprintField = typeof(MiniMapUi).GetField(
                "cameraFootprintView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo obstacleField = typeof(MiniMapUi).GetField(
                "obstacleView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo mapImageField = typeof(MiniMapUi).GetField(
                "mapImage",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(mapImageField, Is.Not.Null);
            Assert.That(footprintField, Is.Not.Null);
            Assert.That(obstacleField, Is.Not.Null);

            try
            {
                mapImageField.SetValue(miniMapUi, mapImage);
                footprintField.SetValue(miniMapUi, footprintView);
                obstacleField.SetValue(miniMapUi, obstacleView);
                miniMapUi.OnPointerExit(null);
                DOTween.Complete(mapImage);

                Assert.That(mapImage.color.a, Is.GreaterThanOrEqualTo(0.75f));
            }
            finally
            {
                DOTween.Kill(mapImage);
                DOTween.Kill(footprintView);
                DOTween.Kill(obstacleView);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class FakeInputService : IPointerGestures, IPointerInput, ISelectionInput
        {
            public event Action<Vector2> WorldPressed;

            public event Action<Vector2> WorldClicked { add { } remove { } }

            public event Action<Vector2> WorldCommanded { add { } remove { } }

            public event Action<Vector2> DragStarted { add { } remove { } }

            public event Action<Vector2> DragChanged { add { } remove { } }

            public event Action<Vector2> DragEnded { add { } remove { } }
            public event Action DragCanceled { add { } remove { } }

            public event Action<Vector2> PrimaryPressed { add { } remove { } }

            public event Action<Vector2> PrimaryReleased { add { } remove { } }

            public event Action SelectVisibleRequested { add { } remove { } }

            public event Action SelectAllRequested { add { } remove { } }

            public Vector2 Position => Vector2.zero;
            public int ClickCount => 1;
            public bool HasPosition => true;

            public void RaiseSelectionBegan(Vector2 position)
            {
                WorldPressed?.Invoke(position);
            }
        }

        private sealed class FakeSelectionQuery : ISelectionQuery
        {
            private readonly Dictionary<Vector2, List<SelectionEntry>> _selections =
                new Dictionary<Vector2, List<SelectionEntry>>();

            public void Add(Vector2 position, SelectionEntry selection)
            {
                if (!_selections.TryGetValue(position, out List<SelectionEntry> entries))
                {
                    entries = new List<SelectionEntry>();
                    _selections.Add(position, entries);
                }

                entries.Add(selection);
            }

            public bool TryFindAt(
                Vector2 screenPosition,
                out SelectionEntry selection)
            {
                bool found = _selections.TryGetValue(screenPosition, out List<SelectionEntry> entries);
                selection = found ? entries[0] : default;
                return found;
            }

            public void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results)
            {
                if (_selections.TryGetValue(screenPosition, out List<SelectionEntry> entries))
                {
                    foreach (SelectionEntry entry in entries)
                    {
                        results.Add(entry);
                    }
                }
            }

            public void CollectSameShipType(
                SelectionEntry selected,
                ICollection<SelectionEntry> results)
            {
                results.Add(selected);
            }

            public void CollectAllPlayerUnits(ICollection<SelectionEntry> results)
            {
            }

            public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results)
            {
            }

            public void CollectInside(
                MarqueeRectangle rectangle,
                ICollection<SelectionEntry> results)
            {
            }
        }

        private sealed class FakeMarqueeSelectionPresenter :
            IMarqueeSelectionPresenter
        {
            public event Action<MarqueeRectangle> Completed { add { } remove { } }
        }

        private sealed class FakeSelectionCommand : IEntitySelectionFacade
        {
            public SelectionType SelectionType { get; set; }
            public bool IsSelected { get; private set; }

            public void PlayAttackFeedback(UnityEngine.Transform target) => throw new NotSupportedException();
            public void StopAttackFeedback() => throw new NotSupportedException();

            public FakeSelectionCommand(SelectionType selectionType)
            {
                SelectionType = selectionType;
            }

            public void Select(bool isSelected)
            {
                IsSelected = isSelected;
            }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public event Action OnDestroy;
            public event Action OnValueChanged { add { } remove { } }
            public int DeathSubscribers => OnDestroy == null ? 0 : OnDestroy.GetInvocationList().Length;
            public ShipClass ShipClass => ShipClass.Fighter;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => IsDestroyed ? 0f : 100f;
            public float HullPercentage => IsDestroyed ? 0f : 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; private set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => !IsDestroyed;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();

            public void Destroy()
            {
                IsDestroyed = true;
                if (OnDestroy != null) OnDestroy.Invoke();
            }
        }

        private sealed class FakeEntity : GameEntity
        {
            private readonly IEntitySelectionFacade _selectionCommand;

            public long Id { get; }
            public FakeHealth Health { get; } = new FakeHealth();
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner { get; }

            public FakeEntity(
                IEntitySelectionFacade selectionCommand,
                PlayerId owner,
                long id)
            {
                Id = id;
                Owner = owner;
                _selectionCommand = selectionCommand;
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityFacade
            {
                if (_selectionCommand is TCommand command)
                {
                    entityCommand = command;
                    return true;
                }

                entityCommand = default;
                return false;
            }
        }
    }
}
