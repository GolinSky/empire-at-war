using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.BaseEntity;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController : IShipUiPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, EmpireAtWar.IObserver<ISelectionSubject>
    {
        private readonly IUiService _uiService;
        private readonly ISelectionService _selectionService;
        private readonly ShipUiModel _model;
        private readonly ShipAbilityService _abilityService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly List<ShipAbilitySlot> _abilitySlots = new List<ShipAbilitySlot>();

        private ISelectionContext _playerSelectionContext;
        private IShipUi _shipUi;
        private IShipGroupUi _shipGroupUi;
        private bool _isRouteActive;

        public ShipUiController(
            IUiService uiService,
            ISelectionService selectionService,
            ShipUiModel model,
            ISkirmishRouteNavigation routeNavigation,
            ShipAbilityService abilityService)
        {
            _uiService = uiService;
            _selectionService = selectionService;
            _model = model;
            _routeNavigation = routeNavigation;
            _abilityService = abilityService;
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.Content, this);
            _abilityService.TargetingChanged += UpdateTargeting;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.Content, this);
            _abilityService.TargetingChanged -= UpdateTargeting;
            if (_shipUi != null)
            {
                _shipUi.Dispose();
                _shipGroupUi.Dispose();
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_shipUi == null)
            {
                _shipUi = (IShipUi)_uiService.CreateUi(UiType.Ship, parentTransform);
                _shipGroupUi = (IShipGroupUi)_uiService.CreateUi(UiType.ShipGroup, parentTransform);

                _shipUi.SetModel(_model);
                _shipUi.SetPresenter(this);
                _shipUi.Initialize();

                _shipGroupUi.SetModel(_model);
                _shipGroupUi.SetPresenter(this);
                _shipGroupUi.Initialize();
            }
            else
            {
                _shipUi.SetParent(parentTransform);
                _shipGroupUi.SetParent(parentTransform);
            }

            _isRouteActive = isActive;
            RefreshSelection();
        }

        public void CloseSelection()
        {
            if (_playerSelectionContext != null)
            {
                _selectionService.RemoveSelectable(_playerSelectionContext);
            }
        }

        public void SelectShipGroup(ShipType shipType)
        {
            _selectionService.SelectCurrentShipsByType(shipType);
        }

        public void PressAbility(ShipAbilityId id) =>
            _abilityService.Press(_playerSelectionContext.Entities, id);

        private void UpdateTargeting() => _model.SetPendingAbility(
            _abilityService.IsWaitingForTarget ? _abilityService.PendingAbilityId : (ShipAbilityId?)null);

        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedType != PlayerType.Player)
            {
                return;
            }

            _playerSelectionContext = subject.PlayerSelectionContext;
            _abilityService.CancelTargeting();
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            bool hasShips = HasMovableSelection() &&
                            _playerSelectionContext.SelectionType == SelectionType.Ship;
            ShipType? selectedShipType = null;
            if (hasShips && _playerSelectionContext.Count == 1 &&
                _playerSelectionContext.Entity.Model is IShipModelObserver ship)
            {
                selectedShipType = ship.ShipType;
            }

            _model.UpdateSelection(hasShips, selectedShipType);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_shipUi == null)
            {
                return;
            }

            bool hasGroup = _model.HasShips && _playerSelectionContext.Count > 1;
            _abilitySlots.Clear();
            _shipGroupUi.ClearGroups();
            SortedDictionary<ShipType, List<IEntity>> groups = new SortedDictionary<ShipType, List<IEntity>>();
            if (_model.HasShips)
            {
                foreach (IEntity entity in _playerSelectionContext.Entities)
                {
                    if (entity.HealthModel.IsDestroyed || !(entity.Model is IShipModelObserver ship))
                        continue;
                    if (entity.TryGetCommand(out IShipAbilityCommand abilityCommand))
                        _abilitySlots.AddRange(abilityCommand.Slots);
                    if (!hasGroup) continue;
                    if (!groups.TryGetValue(ship.ShipType, out List<IEntity> ships))
                    {
                        ships = new List<IEntity>();
                        groups.Add(ship.ShipType, ships);
                    }
                    ships.Add(entity);
                }
            }
            _shipUi.SetAbilitySlots(_abilitySlots);
            _shipUi.SetHealth(_model.HasShips && !hasGroup
                ? _playerSelectionContext.Entity.HealthModel : null);

            foreach (KeyValuePair<ShipType, List<IEntity>> group in groups)
            {
                List<ShipUiEntry> entries = new List<ShipUiEntry>();
                foreach (IEntity entity in group.Value)
                {
                    IEntity[] caster = { entity };
                    IReadOnlyList<ShipAbilitySlot> slots = entity.TryGetCommand(out IShipAbilityCommand command)
                        ? command.Slots : System.Array.Empty<ShipAbilitySlot>();
                    entries.Add(new ShipUiEntry(slots, id => _abilityService.Press(caster, id),
                        entity.HealthModel));
                }
                List<IEntity> casters = group.Value;
                _shipGroupUi.AddGroup(group.Key, entries, id => _abilityService.Press(casters, id));
            }

            if (_isRouteActive && _model.HasShips && !hasGroup)
            {
                _shipUi.Show();
                _shipGroupUi.Hide();
            }
            else if (_isRouteActive && hasGroup)
            {
                _shipUi.Hide();
                _shipGroupUi.Show();
            }
            else
            {
                _shipUi.Hide();
                _shipGroupUi.Hide();
            }
        }

        private bool HasMovableSelection()
        {
            if (_playerSelectionContext == null)
            {
                return false;
            }

            for (int i = 0; i < _playerSelectionContext.Entities.Count; i++)
            {
                if (!_playerSelectionContext.Entities[i].HealthModel.IsDestroyed &&
                    _playerSelectionContext.Entities[i].TryGetCommand(out IMoveCommand _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
