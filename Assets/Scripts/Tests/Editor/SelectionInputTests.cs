using System;
using EmpireAtWar.Tests.Editor;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Views.MiniMap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Tests.Selection
{
    public sealed class SelectionInputTests
    {
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
                    new FakeEntity(1, TestPlayers.Human, playerCommand),
                    playerCommand));
            selectionQuery.Add(
                opponentPosition,
                new SelectionEntry(
                    new FakeEntity(2, TestPlayers.Enemy, opponentCommand),
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
            public event Action<Vector2> PrimaryPressed { add { } remove { } }
            public event Action<Vector2> PrimaryReleased { add { } remove { } }
            public event Action SelectVisibleRequested { add { } remove { } }
            public event Action SelectAllRequested { add { } remove { } }

            public Vector2 Position => Vector2.zero;
            public int ClickCount => 1;

            public void RaiseSelectionBegan(Vector2 position)
            {
                WorldPressed?.Invoke(position);
            }
        }

        private sealed class FakeSelectionQuery : ISelectionQuery
        {
            private readonly Dictionary<Vector2, SelectionEntry> _selections =
                new Dictionary<Vector2, SelectionEntry>();

            public void Add(Vector2 position, SelectionEntry selection)
            {
                _selections.Add(position, selection);
            }

            public bool TryFindAt(
                Vector2 screenPosition,
                out SelectionEntry selection)
            {
                return _selections.TryGetValue(screenPosition, out selection);
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
            public FakeSelectionCommand(SelectionType selectionType)
            {
                SelectionType = selectionType;
            }

            public SelectionType SelectionType { get; set; }
            public bool IsSelected { get; private set; }

            public void Select(bool isSelected)
            {
                IsSelected = isSelected;
            }
        }

        private sealed class FakeEntity : GameEntity
        {
            private readonly IEntitySelectionFacade _selectionCommand;

            public FakeEntity(
                long id,
                PlayerId owner,
                IEntitySelectionFacade selectionCommand)
            {
                Id = id;
                Owner = owner;
                _selectionCommand = selectionCommand;
            }

            public long Id { get; }
            public IHealthModelObserver HealthModel => null;
            public PlayerId Owner { get; }

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
