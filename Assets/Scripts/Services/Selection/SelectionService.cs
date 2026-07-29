using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionService : IService, INotifier<ISelectionSubject>
    {
        void RemoveSelectable(ISelectionContext selectionContext);
    }

    public sealed class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable,
        ISelectionSubject
    {
        private readonly IInputService _inputService;
        private readonly IEntityLocator _entityLocator;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IMarqueeSelectionPresenter _marqueeSelectionPresenter;
        private readonly List<IObserver<ISelectionSubject>> _observers =
            new List<IObserver<ISelectionSubject>>();
        private readonly List<SelectionEntry> _selectionBuffer = new List<SelectionEntry>();
        private readonly List<IAttackCommand> _attackCommands = new List<IAttackCommand>();
        private readonly List<FormationPoint> _attackFormationPositions =
            new List<FormationPoint>();
        private readonly List<float> _attackFormationRadii =
            new List<float>();
        private readonly List<FormationPoint> _attackFormationOffsets =
            new List<FormationPoint>();
        private readonly SelectionContext _playerSelectionContext = new SelectionContext(PlayerType.Player);
        private readonly SelectionContext _enemySelectionContext = new SelectionContext(PlayerType.Opponent);
        private long? _lastTappedEntityId;

        public ISelectionContext PlayerSelectionContext => _playerSelectionContext;
        public ISelectionContext EnemySelectionContext => _enemySelectionContext;
        public PlayerType UpdatedType { get; private set; }

        public SelectionService(
            IInputService inputService,
            IEntityLocator entityLocator,
            ISelectionQuery selectionQuery,
            IMarqueeSelectionPresenter marqueeSelectionPresenter)
        {
            _inputService = inputService;
            _entityLocator = entityLocator;
            _selectionQuery = selectionQuery;
            _marqueeSelectionPresenter = marqueeSelectionPresenter;
        }

        public void Initialize()
        {
            _inputService.OnInput += HandleInput;
            _marqueeSelectionPresenter.Completed += HandleMarqueeCompleted;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _inputService.OnInput -= HandleInput;
            _marqueeSelectionPresenter.Completed -= HandleMarqueeCompleted;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            _playerSelectionContext.ResetCurrentSelectable();
            _enemySelectionContext.ResetCurrentSelectable();
        }

        public void RemoveSelectable(ISelectionContext context)
        {
            if (context == null)
            {
                return;
            }

            ClearSelection(context.PlayerType);
        }

        private void HandleInput(InputType inputType, TouchPhase touchPhase, Vector2 touchPosition)
        {
            if (inputType == InputType.ShipInput)
            {
                HandleActionInput(touchPosition);
                return;
            }

            if (inputType != InputType.Selection)
            {
                return;
            }

            if (!_selectionQuery.TryFindAt(touchPosition, out SelectionEntry selection))
            {
                _lastTappedEntityId = null;
                return;
            }

            bool isRepeatedTap = _lastTappedEntityId == selection.Entity.Id;
            _lastTappedEntityId = selection.Entity.Id;
            if (isRepeatedTap &&
                selection.Entity.PlayerType == PlayerType.Player &&
                _inputService.TapCount >= 2 &&
                TryCollectSameShipType(selection))
            {
                SetSelection(selection.Entity.PlayerType, _selectionBuffer);
                return;
            }

            _selectionBuffer.Clear();
            _selectionBuffer.Add(selection);
            SetSelection(selection.Entity.PlayerType, _selectionBuffer);
        }

        private void HandleActionInput(Vector2 touchPosition)
        {
            if (_selectionQuery.TryFindAt(touchPosition, out SelectionEntry target) &&
                target.Entity.PlayerType == PlayerType.Opponent)
            {
                DispatchAttack(target.Entity);
            }
        }

        private bool TryCollectSameShipType(SelectionEntry selection)
        {
            _selectionBuffer.Clear();
            _selectionQuery.CollectSameShipType(selection, _selectionBuffer);
            return _selectionBuffer.Count > 0;
        }

        private void HandleMarqueeCompleted(MarqueeRectangle rectangle)
        {
            _lastTappedEntityId = null;
            _selectionBuffer.Clear();
            _selectionQuery.CollectInside(rectangle, _selectionBuffer);
            SetSelection(PlayerType.Player, _selectionBuffer);
        }

        private void SetSelection(PlayerType playerType, IReadOnlyList<SelectionEntry> selection)
        {
            SelectionContext context = GetContext(playerType);
            if (context == null)
            {
                return;
            }

            context.Replace(selection);
            NotifyObservers(playerType);
        }

        private void ClearSelection(PlayerType playerType)
        {
            SelectionContext context = GetContext(playerType);
            if (context == null)
            {
                return;
            }

            context.ResetCurrentSelectable();
            NotifyObservers(playerType);
        }

        private void DispatchAttack(IEntity target)
        {
            _attackCommands.Clear();
            _attackFormationPositions.Clear();
            _attackFormationRadii.Clear();
            IReadOnlyList<IEntity> selectedEntities =
                _playerSelectionContext.Entities;
            for (int i = 0; i < selectedEntities.Count; i++)
            {
                IEntity entity = selectedEntities[i];
                if (entity.HealthModel.IsDestroyed ||
                    !entity.TryGetCommand(out IAttackCommand attackCommand))
                {
                    continue;
                }

                _attackCommands.Add(attackCommand);
                _attackFormationPositions.Add(new FormationPoint(
                    attackCommand.WorldPosition.x,
                    attackCommand.WorldPosition.z));
                _attackFormationRadii.Add(
                    attackCommand.NavigationRadius);
            }

            Vector3 targetPosition = target.HealthModel.Transform.position;
            FormationPoint targetCenter = new FormationPoint(
                targetPosition.x,
                targetPosition.z);
            FormationModel.CalculateCompactDestinations(
                _attackFormationPositions,
                _attackFormationRadii,
                targetCenter,
                _attackFormationOffsets);
            for (int i = 0; i < _attackCommands.Count; i++)
            {
                FormationPoint destination =
                    _attackFormationOffsets[i];
                _attackCommands[i].Attack(
                    target,
                    new Vector3(
                        destination.X - targetCenter.X,
                        0f,
                        destination.Z - targetCenter.Z));
            }
        }

        private void HandleEntityRemoved(EmpireAtWar.Entities.BaseEntity.IEntity entity)
        {
            if (_lastTappedEntityId == entity.Id)
            {
                _lastTappedEntityId = null;
            }

            SelectionContext context = GetContext(entity.PlayerType);
            if (context != null && context.Remove(entity))
            {
                NotifyObservers(entity.PlayerType);
            }
        }

        private SelectionContext GetContext(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player:
                    return _playerSelectionContext;
                case PlayerType.Opponent:
                    return _enemySelectionContext;
                default:
                    return null;
            }
        }

        private void NotifyObservers(PlayerType playerType)
        {
            UpdatedType = playerType;
            for (int i = 0; i < _observers.Count; i++)
            {
                _observers[i].UpdateState(this);
            }
        }

        public void AddObserver(IObserver<ISelectionSubject> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void RemoveObserver(IObserver<ISelectionSubject> observer)
        {
            _observers.Remove(observer);
        }
    }
}
