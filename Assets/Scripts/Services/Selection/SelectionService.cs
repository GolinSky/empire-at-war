using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Input;
using UnityEngine;
using Zenject;
using IEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SelectionService : Service, ISelectionService, IInitializable, ILateDisposable,
        ISelectionSubject
    {
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
        private readonly SelectionContext _playerSelectionContext;
        private readonly SelectionContext _otherSelectionContext;

        private long? _lastTappedEntityId;

        public ISelectionContext PlayerSelectionContext => _playerSelectionContext;
        public ISelectionContext OtherSelectionContext => _otherSelectionContext;
        public SelectionScope UpdatedScope { get; private set; }

        public SelectionService(
            IPointerGestures gestures,
            IPointerInput pointerInput,
            ISelectionInput selectionInput,
            IEntityLocator entityLocator,
            ISelectionQuery selectionQuery,
            IMarqueeSelectionPresenter marqueeSelectionPresenter,
            ILocalPlayer localPlayer)
        {
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
            _selectionInput.SelectAllRequested += HandleSelectAllUnitsPressed;
            _selectionInput.SelectVisibleRequested += HandleSelectVisibleUnitsPressed;
            _marqueeSelectionPresenter.Completed += HandleMarqueeCompleted;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _gestures.WorldPressed -= HandleWorldPressed;
            _selectionInput.SelectAllRequested -= HandleSelectAllUnitsPressed;
            _selectionInput.SelectVisibleRequested -= HandleSelectVisibleUnitsPressed;
            _marqueeSelectionPresenter.Completed -= HandleMarqueeCompleted;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
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
            ResetAllSelections();
            SelectAt(screenPosition);
        }

        private void SelectAt(Vector2 screenPosition)
        {
            if (!_selectionQuery.TryFindAt(screenPosition, out SelectionEntry selection))
            {
                _lastTappedEntityId = null;
                return;
            }

            bool isRepeatedTap = _lastTappedEntityId == selection.Entity.Id;
            _lastTappedEntityId = selection.Entity.Id;
            if (isRepeatedTap &&
                _localPlayer.IsLocal(selection.Entity.Owner) &&
                _pointerInput.ClickCount >= 2 &&
                TryCollectSameShipType(selection))
            {
                SetSelection(GetScope(selection.Entity), _selectionBuffer);
                return;
            }

            _selectionBuffer.Clear();
            _selectionBuffer.Add(selection);
            SetSelection(GetScope(selection.Entity), _selectionBuffer);
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
            GetContext(scope).Replace(selection);
            NotifyObservers(scope);
        }

        private void ClearSelection(SelectionScope scope)
        {
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

        private void HandleEntityRemoved(EmpireAtWar.Entities.BaseEntity.IEntity entity)
        {
            if (_lastTappedEntityId == entity.Id)
            {
                _lastTappedEntityId = null;
            }

            SelectionScope scope = GetScope(entity);
            if (GetContext(scope).Remove(entity))
            {
                NotifyObservers(scope);
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

        private void NotifyObservers(SelectionScope scope)
        {
            UpdatedScope = scope;
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
