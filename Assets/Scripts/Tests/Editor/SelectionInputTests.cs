using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
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
                new EntityLocator(),
                selectionQuery,
                marqueeSelectionPresenter);
            FakeSelectionCommand playerCommand =
                new FakeSelectionCommand(SelectionType.Ship);
            FakeSelectionCommand opponentCommand =
                new FakeSelectionCommand(SelectionType.Ship);
            selectionQuery.Add(
                playerPosition,
                new SelectionEntry(
                    new FakeEntity(1, PlayerType.Player, playerCommand),
                    playerCommand));
            selectionQuery.Add(
                opponentPosition,
                new SelectionEntry(
                    new FakeEntity(2, PlayerType.Opponent, opponentCommand),
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
                    selectionService.EnemySelectionContext.HasSelectable,
                    Is.True);

                inputService.RaiseSelectionBegan(emptyPosition);

                Assert.That(opponentCommand.IsSelected, Is.False);
                Assert.That(
                    selectionService.EnemySelectionContext.HasSelectable,
                    Is.False);
            }
            finally
            {
                selectionService.LateDispose();
            }
        }

        [Test]
        public void DisabledMiniMapInteraction_KeepsMapVisible()
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
            FieldInfo mapImageField = typeof(MiniMapUi).GetField(
                "mapImage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo activateInteractionMethod = typeof(MiniMapUi).GetMethod(
                "ActivateInteraction",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(mapImageField, Is.Not.Null);
            Assert.That(activateInteractionMethod, Is.Not.Null);

            try
            {
                mapImageField.SetValue(miniMapUi, mapImage);
                activateInteractionMethod.Invoke(miniMapUi, new object[] { false });
                DOTween.Complete(mapImage);

                Assert.That(mapImage.color.a, Is.EqualTo(1f));
            }
            finally
            {
                DOTween.Kill(mapImage);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class FakeInputService : IInputService
        {
            public event Action<Vector2> OnSwipe;
            public event Action<Vector2> OnCameraPan;
            public event Action OnLeftMousePressed;
            public event Action<Vector2> OnPrimaryDragStarted;
            public event Action<Vector2> OnPrimaryDragChanged;
            public event Action<Vector2> OnPrimaryDragEnded;
            public event Action OnEscapePressed;
            public event Action<bool> OnBlocked;
            public event Action<InputType, TouchPhase, Vector2> OnInput;
            public event Action<Vector2> OnEndDrag;
            public event Action<float> OnZoom;

            public TouchPhase CurrentTouchPhase => TouchPhase.Began;
            public Vector2 TouchPosition => Vector2.zero;
            public bool SupportsHover => true;
            public Vector2 CameraMove => Vector2.zero;
            public int TapCount => 1;
            public string Id { get; set; }

            public void RaiseSelectionBegan(Vector2 position)
            {
                OnInput?.Invoke(InputType.Selection, TouchPhase.Began, position);
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

            public void CollectInside(
                MarqueeRectangle rectangle,
                ICollection<SelectionEntry> results)
            {
            }
        }

        private sealed class FakeMarqueeSelectionPresenter :
            IMarqueeSelectionPresenter
        {
            public event Action<MarqueeRectangle> Completed;
        }

        private sealed class FakeSelectionCommand : IEntitySelectionCommand
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
            private readonly IEntitySelectionCommand _selectionCommand;

            public FakeEntity(
                long id,
                PlayerType playerType,
                IEntitySelectionCommand selectionCommand)
            {
                Id = id;
                PlayerType = playerType;
                _selectionCommand = selectionCommand;
            }

            public long Id { get; }
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel => null;
            public PlayerType PlayerType { get; }

            public bool TryGetCommand<TCommand>(out TCommand entityCommand)
                where TCommand : IEntityCommand
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
