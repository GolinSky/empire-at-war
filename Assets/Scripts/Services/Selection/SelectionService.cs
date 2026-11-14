using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Selection;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable,
        ISelectionSubject
    {
        private const float REPEATED_TAP_RADIUS = 12f;

        private readonly IPointerGestures _gestures;
        private readonly IPointerInput _pointerInput;
        private readonly ISelectionInput _selectionInput;
        private readonly IEntityLocator _entityLocator;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IMarqueeSelectionPresenter _marqueeSelectionPresenter;
        private readonly ILocalPlayer _localPlayer;

        private readonly List<IObserver<ISelectionSubject>> _observers =
            new List<IObserver<ISelectionSubject>>();
        private readonly List<SelectionEntry> _selectionBuffer = new List<SelectionEntry>();
        private readonly HashSet<IEntity> _deathSubscriptions = new HashSet<IEntity>();
        private readonly System.Action _selectedEntityDestroyed;
        private readonly SelectionContext _playerSelectionContext;
        private readonly SelectionContext _otherSelectionContext;

        private readonly List<SelectionEntry> _pointBuffer = new List<SelectionEntry>();
        private SelectionEntry _pressedMarker;

        private long? _lastTappedEntityId;
        private Vector2 _lastTappedPosition;

        public ISelectionContext PlayerSelectionContext => _playerSelectionContext;
        public ISelectionContext OtherSelectionContext => _otherSelectionContext;
        public SelectionScope UpdatedScope { get; private set; }
        public bool IsPruningSelection { get; private set; }

        public SelectionService(
            IPointerGestures gestures,
            IPointerInput pointerInput,
            ISelectionInput selectionInput,
            IEntityLocator entityLocator,
            ISelectionQuery selectionQuery,
            IMarqueeSelectionPresenter marqueeSelectionPresenter,
            ILocalPlayer localPlayer)
        {
            _selectedEntityDestroyed = HandleSelectedEntityDestroyed;
            _localPlayer = localPlayer;
            _playerSelectionContext = new SelectionContext(scope: SelectionScope.Local, localPlayer: localPlayer);
            _otherSelectionContext = new SelectionContext(scope: SelectionScope.Other, localPlayer: localPlayer);
            _gestures = gestures;
            _pointerInput = pointerInput;
            _selectionInput = selectionInput;
            _entityLocator = entityLocator;
            _selectionQuery = selectionQuery;
            _marqueeSelectionPresenter = marqueeSelectionPresenter;
        }

        public void Initialize()
        {
            _gestures.WorldPressed += HandleWorldPressed;
            _gestures.WorldClicked += HandleWorldClicked;
            _gestures.DragStarted += HandleDragStarted;
            _selectionInput.SelectAllRequested += HandleSelectAllUnitsPressed;
            _selectionInput.SelectVisibleRequested += HandleSelectVisibleUnitsPressed;
            _marqueeSelectionPresenter.Completed += HandleMarqueeCompleted;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _gestures.WorldPressed -= HandleWorldPressed;
            _gestures.WorldClicked -= HandleWorldClicked;
            _gestures.DragStarted -= HandleDragStarted;
            _pressedMarker = default;
            _selectionInput.SelectAllRequested -= HandleSelectAllUnitsPressed;
            _selectionInput.SelectVisibleRequested -= HandleSelectVisibleUnitsPressed;
            _marqueeSelectionPresenter.Completed -= HandleMarqueeCompleted;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            foreach (IEntity entity in _deathSubscriptions)
                entity.HealthModel.OnDestroy -= _selectedEntityDestroyed;
            _deathSubscriptions.Clear();
            _playerSelectionContext.ResetCurrentSelectable();
            _otherSelectionContext.ResetCurrentSelectable();
        }

        public void RemoveSelectable(ISelectionContext context)
        {
            if (context == null)
            {
                return;
            }

            ClearSelection(context.Scope);
        }

        public void SelectEntity(IEntity entity)
        {
            if (entity.HealthModel.IsDestroyed || !entity.TryGetFacade(out IEntitySelectionFacade command))
                return;

            _lastTappedEntityId = null;
            ResetAllSelections();
            _selectionBuffer.Clear();
            _selectionBuffer.Add(new SelectionEntry(entity, command));
            SetSelection(GetScope(entity), _selectionBuffer);
        }

        public void SelectCurrentUnitsByType(UnitTypeId unitTypeId)
        {
            _selectionBuffer.Clear();
            foreach (IEntity entity in _playerSelectionContext.Entities)
            {
                if (entity.IsUnitType(unitTypeId) &&
                    !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IEntitySelectionFacade command))
                {
                    _selectionBuffer.Add(new SelectionEntry(entity, command));
                }
            }

            if (_selectionBuffer.Count == 0)
            {
                return;
            }

            _lastTappedEntityId = null;
            SetSelection(SelectionScope.Local, _selectionBuffer);
        }

        private void HandleWorldPressed(Vector2 screenPosition)
        {
            _pressedMarker = default;
            if (!_selectionQuery.TryFindMarkerAt(screenPosition, out _))
                return;

            _pointBuffer.Clear();
            _selectionQuery.CollectAt(screenPosition, _pointBuffer);
            int index = GetPointSelectionIndex(screenPosition);
            if (index >= 0 && _pointBuffer[index].IsBattleMarker)
                _pressedMarker = _pointBuffer[index];
        }

        private void HandleDragStarted(Vector2 _) => _pressedMarker = default;

        private void HandleWorldClicked(Vector2 screenPosition)
        {
            ResetAllSelections();
            SelectionEntry selection = _pressedMarker;
            _pressedMarker = default;
            if (selection.IsBattleMarker)
            {
                if (!_selectionQuery.IsMarkerAvailable(selection))
                {
                    _lastTappedEntityId = null;
                    return;
                }
            }
            else
            {
                _pointBuffer.Clear();
                _selectionQuery.CollectAt(screenPosition, _pointBuffer);
                int index = GetPointSelectionIndex(screenPosition);
                if (index < 0)
                {
                    _lastTappedEntityId = null;
                    return;
                }
                selection = _pointBuffer[index];
            }

            if (IsRepeatedTap(screenPosition) && selection.Entity.Id == _lastTappedEntityId &&
                _pointerInput.ClickCount >= 2 && _localPlayer.IsLocal(selection.Entity.Owner) &&
                TryCollectSameShipType(selection))
            {
                SetSelection(GetScope(selection.Entity), _selectionBuffer);
                return;
            }

            _lastTappedEntityId = selection.Entity.Id;
            _lastTappedPosition = screenPosition;
            _selectionBuffer.Clear();
            _selectionBuffer.Add(selection);
            SetSelection(GetScope(selection.Entity), _selectionBuffer);
        }

        private int GetPointSelectionIndex(Vector2 screenPosition)
        {
            if (_pointBuffer.Count == 0)
                return -1;

            int lastIndex = IsRepeatedTap(screenPosition) ? IndexOf(_pointBuffer, _lastTappedEntityId) : -1;
            if (lastIndex < 0)
                return FindInitialSelectionIndex();

            // Repeated single clicks cycle through overlapping hits.
            return _pointerInput.ClickCount >= 2 ? lastIndex : (lastIndex + 1) % _pointBuffer.Count;
        }

        private bool IsRepeatedTap(Vector2 screenPosition) =>
            (screenPosition - _lastTappedPosition).sqrMagnitude <= REPEATED_TAP_RADIUS * REPEATED_TAP_RADIUS;

        private int FindInitialSelectionIndex()
        {
            int index = _pointBuffer.FindIndex(entry => _localPlayer.IsLocal(entry.Entity.Owner));
            bool hasLocalHit = index >= 0;
            if (!hasLocalHit)
                index = 0;

            SelectionEntry first = _pointBuffer[index];
            if (!first.IsBattleMarker && first.Command.SelectionType == SelectionType.Ship &&
                _localPlayer.IsFriendly(first.Entity.Owner))
            {
                // A friendly capital ship's hull must not hide a mine's selection target.
                for (int i = index + 1; i < _pointBuffer.Count; i++)
                {
                    SelectionEntry candidate = _pointBuffer[i];
                    if (candidate.Command.SelectionType == SelectionType.MiningFacility &&
                        (!hasLocalHit || _localPlayer.IsLocal(candidate.Entity.Owner)))
                        return i;
                }
            }

            return index;
        }

        private static int IndexOf(IReadOnlyList<SelectionEntry> entries, long? entityId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Entity.Id == entityId)
                {
                    return i;
                }
            }

            return -1;
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
            if (_otherSelectionContext.HasSelectable)
                ClearSelection(SelectionScope.Other);
            SetSelection(SelectionScope.Local, _selectionBuffer);
        }

        private void HandleSelectAllUnitsPressed()
        {
            _lastTappedEntityId = null;
            _selectionBuffer.Clear();
            _selectionQuery.CollectAllPlayerUnits(_selectionBuffer);
            ResetAllSelections();
            SetSelection(SelectionScope.Local, _selectionBuffer);
        }

        private void HandleSelectVisibleUnitsPressed()
        {
            _lastTappedEntityId = null;
            _selectionBuffer.Clear();
            _selectionQuery.CollectVisiblePlayerUnits(_selectionBuffer);
            ResetAllSelections();
            SetSelection(SelectionScope.Local, _selectionBuffer);
        }

        private void SetSelection(SelectionScope scope, IReadOnlyList<SelectionEntry> selection)
        {
            foreach (IEntity entity in GetContext(scope).Entities)
                UnsubscribeDeath(entity);
            GetContext(scope).Replace(selection);
            foreach (IEntity entity in GetContext(scope).Entities)
            {
                _deathSubscriptions.Add(entity);
                entity.HealthModel.OnDestroy += _selectedEntityDestroyed;
            }
            NotifyObservers(scope);
        }

        private void ClearSelection(SelectionScope scope)
        {
            foreach (IEntity entity in GetContext(scope).Entities)
                UnsubscribeDeath(entity);
            GetContext(scope).ResetCurrentSelectable();
            NotifyObservers(scope);
        }

        private void ResetAllSelections()
        {
            if (_playerSelectionContext.HasSelectable)
            {
                ClearSelection(SelectionScope.Local);
            }

            if (_otherSelectionContext.HasSelectable)
            {
                ClearSelection(SelectionScope.Other);
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            UnsubscribeDeath(entity);
            if (_lastTappedEntityId == entity.Id)
            {
                _lastTappedEntityId = null;
            }

            SelectionScope scope = GetScope(entity);
            if (GetContext(scope).Remove(entity))
            {
                NotifyObservers(scope, isPruningSelection: true);
            }
        }

        private void UnsubscribeDeath(IEntity entity)
        {
            if (!_deathSubscriptions.Remove(entity)) return;
            entity.HealthModel.OnDestroy -= _selectedEntityDestroyed;
        }

        private void HandleSelectedEntityDestroyed()
        {
            PruneDestroyed(_playerSelectionContext);
            PruneDestroyed(_otherSelectionContext);
        }

        private void PruneDestroyed(SelectionContext context)
        {
            for (int i = context.Count - 1; i >= 0; i--)
            {
                IEntity entity = context.Entities[i];
                if (entity.HealthModel.IsDestroyed)
                    HandleEntityRemoved(entity);
            }
        }

        private SelectionScope GetScope(IEntity entity)
        {
            return _localPlayer.IsLocal(entity.Owner) ? SelectionScope.Local : SelectionScope.Other;
        }

        private SelectionContext GetContext(SelectionScope scope)
        {
            return scope == SelectionScope.Local ? _playerSelectionContext : _otherSelectionContext;
        }

        private void NotifyObservers(SelectionScope scope, bool isPruningSelection = false)
        {
            UpdatedScope = scope;
            IsPruningSelection = isPruningSelection;
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
