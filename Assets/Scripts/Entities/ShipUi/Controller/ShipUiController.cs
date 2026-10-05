using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using System;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.ShipUi;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController : UiController, IShipUiPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, EmpireAtWar.IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selectionService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly ICameraService _cameraService;
        private ISelectionContext _playerSelectionContext;
        private IShipUi _shipUi;
        private IShipGroupUi _shipGroupUi;
        private readonly EmpireAtWar.Services.Input.IInputBindings _bindings;

        private readonly ShipUiModel _model;
        private readonly ShipAbilityService _shipAbilityService;
        private readonly List<ShipAbilitySlot> _abilitySlots = new List<ShipAbilitySlot>();
        private readonly TooltipRequests _tooltips;
        private readonly TooltipIconData _tooltipIcons;
        private readonly EmpireAtWar.Models.Factions.FactionCatalog _factions;
        private TooltipHoverSubscription _shipTooltipHover;
        private TooltipHoverSubscription _groupTooltipHover;

        private bool _isRouteActive;

        public ShipUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISelectionService selectionService,
            ISkirmishRouteNavigation routeNavigation,
            ICameraService cameraService,
            ITooltipService tooltipService,
            EmpireAtWar.Services.Input.IInputBindings bindings,
            ShipUiModel model,
            ShipAbilityService shipAbilityService,
            TooltipIconData tooltipIcons,
            EmpireAtWar.Models.Factions.FactionCatalog factions) : base(uiService, cancelRouter)
        {
            _selectionService = selectionService;
            _model = model;
            _routeNavigation = routeNavigation;
            _shipAbilityService = shipAbilityService;
            _cameraService = cameraService;
            _tooltips = new TooltipRequests(tooltipService);
            _tooltipIcons = tooltipIcons;
            _factions = factions;
            _bindings = bindings;
        }

        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.Content, this);
            _shipAbilityService.TargetingChanged += UpdateTargeting;
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.Content, this);
            _shipAbilityService.TargetingChanged -= UpdateTargeting;
            if (_shipUi != null)
            {
                _shipTooltipHover.Dispose();
                _groupTooltipHover.Dispose();
                _shipUi.Dispose();
                _shipGroupUi.Dispose();
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_shipUi == null)
            {
                _shipUi = (IShipUi)UiService.CreateUi(UiType.Ship, parentTransform);
                _shipGroupUi = (IShipGroupUi)UiService.CreateUi(UiType.ShipGroup, parentTransform);

                _shipUi.SetModel(_model);
                _shipUi.SetPresenter(this);
                _shipUi.Initialize();

                _shipGroupUi.SetModel(_model);
                _shipGroupUi.SetPresenter(this);
                _shipGroupUi.Initialize();
                _shipTooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_shipUi).TooltipHover, HandleTooltipHover, _tooltips);
                _groupTooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_shipGroupUi).TooltipHover, HandleTooltipHover, _tooltips);
            }
            else
            {
                _shipUi.SetParent(parentTransform);
                _shipGroupUi.SetParent(parentTransform);
            }

            _isRouteActive = isActive;
            if (!isActive) _tooltips.HideAll();
            RefreshSelection();
        }

        public void CloseSelection()
        {
            if (_playerSelectionContext != null)
            {
                _selectionService.RemoveSelectable(_playerSelectionContext);
            }
        }

        public void FocusSelection() => FocusEntity(_playerSelectionContext.Entity);

        private void FocusEntity(IEntity entity) =>
            _cameraService.MoveTo(entity.GetFacade<IMoveFacade>().WorldPosition);

        public void SelectShipGroup(ShipType shipType)
        {
            _selectionService.SelectCurrentUnitsByType(UnitTypeId.Ship(shipType));
        }

        public void SelectSquadronGroup(SquadronType squadronType)
        {
            _selectionService.SelectCurrentUnitsByType(UnitTypeId.Squadron(squadronType));
        }

        public void PressAbility(ShipAbilityId id) =>
            _shipAbilityService.Press(_playerSelectionContext.Entities, id);

        private void UpdateTargeting() => _model.SetPendingAbility(
            _shipAbilityService.IsWaitingForTarget ? _shipAbilityService.PendingAbilityId : (ShipAbilityId?)null);

        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedScope != SelectionScope.Local)
            {
                return;
            }

            _playerSelectionContext = subject.PlayerSelectionContext;
            _tooltips.HideAll();
            _shipAbilityService.CancelTargeting();
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            bool hasShips = HasMovableSelection() &&
                            _playerSelectionContext.SelectionType == SelectionType.Ship;
            ShipType? selectedShipType = null;
            SquadronType? selectedSquadronType = null;
            if (hasShips && _playerSelectionContext.Count == 1 &&
                _playerSelectionContext.Entity.TryGetFacade(out IUnitTypeFacade unit))
            {
                if (unit.UnitTypeId.IsShip)
                    selectedShipType = unit.UnitTypeId.ShipType;
                else
                    selectedSquadronType = unit.UnitTypeId.SquadronType;
            }

            _model.UpdateSelection(hasShips, selectedShipType, selectedSquadronType);
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
            SortedDictionary<SquadronType, List<IEntity>> squadrons = new SortedDictionary<SquadronType, List<IEntity>>();
            if (_model.HasShips)
            {
                foreach (IEntity entity in _playerSelectionContext.Entities)
                {
                    if (entity.HealthModel.IsDestroyed)
                        continue;
                    if (entity.TryGetFacade(out IShipAbilityFacade abilityCommand))
                        _abilitySlots.AddRange(abilityCommand.Slots);
                    if (!hasGroup || !entity.TryGetFacade(out IUnitTypeFacade unit)) continue;
                    if (unit.UnitTypeId.IsShip)
                        AddToGroup(groups, unit.UnitTypeId.ShipType, entity);
                    else
                        AddToGroup(squadrons, unit.UnitTypeId.SquadronType, entity);
                }
            }
            _shipUi.SetAbilitySlots(_abilitySlots);
            _shipUi.SetHealth(_model.HasShips && !hasGroup
                ? _playerSelectionContext.Entity.HealthModel : null);

            AddSelectionGroups(groups, _shipGroupUi.AddGroup);
            AddSelectionGroups(squadrons, _shipGroupUi.AddGroup);

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

        private static void AddToGroup<T>(SortedDictionary<T, List<IEntity>> groups, T type, IEntity entity)
            where T : struct, Enum
        {
            if (!groups.TryGetValue(type, out List<IEntity> entities))
            {
                entities = new List<IEntity>();
                groups.Add(type, entities);
            }
            entities.Add(entity);
        }

        private void AddSelectionGroups<T>(SortedDictionary<T, List<IEntity>> groups,
            Action<T, IReadOnlyList<ShipUiEntry>, Action<ShipAbilityId>> addGroup) where T : struct, Enum
        {
            foreach (KeyValuePair<T, List<IEntity>> group in groups)
            {
                List<ShipUiEntry> entries = new List<ShipUiEntry>();
                foreach (IEntity entity in group.Value)
                {
                    IReadOnlyList<ShipAbilitySlot> slots = entity.TryGetFacade(out IShipAbilityFacade command)
                        ? command.Slots : Array.Empty<ShipAbilitySlot>();
                    entries.Add(new ShipUiEntry(abilitySlots: slots, health: entity.HealthModel,
                        focus: () => FocusEntity(entity), entity: entity));
                }
                List<IEntity> casters = group.Value;
                addGroup(group.Key, entries, id => _shipAbilityService.Press(casters, id));
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
                    _playerSelectionContext.Entities[i].TryGetFacade(out IMoveFacade _))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isRouteActive && _model.HasShips,
                () => BuildTooltip(key));

        private TooltipContent BuildTooltip(object key)
        {
            if (key is string action && action == "Focus")
                return new TooltipContent(title: "Focus selected ship", description: "Click to move the camera to the selected ship.");
            if (key is string selection && selection == "Selection")
                return new TooltipContent(title: "Clear selection", description: "Click to deselect the current ship.");
            if (key is IReadOnlyList<ShipAbilitySlot> slots)
                return ShipAbilityTooltipContent.Build(slots, _bindings, _tooltipIcons);
            if (key is EmpireAtWar.Models.ShipUi.ShipUiEntry entry)
                return EntityTooltipContent.Build(entry.Entity);
            if (key is ShipType || key is SquadronType)
            {
                UnitTypeId unitTypeId = key is ShipType shipType
                    ? UnitTypeId.Ship(shipType)
                    : UnitTypeId.Squadron((SquadronType)key);
                int count = 0;
                int damaged = 0;
                foreach (IEntity entity in _playerSelectionContext.Entities)
                {
                    if (!entity.IsUnitType(unitTypeId) || entity.HealthModel.IsDestroyed) continue;
                    count++;
                    if (entity.HealthModel.HullPercentage < 1f) damaged++;
                }
                var data = unitTypeId.IsShip
                    ? _factions.GetShipFactionData(unitTypeId.ShipType)
                    : _factions.GetSquadronFactionData(unitTypeId.SquadronType);
                return UnitTooltipContent.Build(data, new[]
                {
                    new TooltipStat(label: "Selected", current: count),
                    new TooltipStat(label: "Damaged", current: damaged)
                }, status: "Click to select this unit type.");
            }
            return EntityTooltipContent.Build(_playerSelectionContext.Entity);
        }
    }
}
