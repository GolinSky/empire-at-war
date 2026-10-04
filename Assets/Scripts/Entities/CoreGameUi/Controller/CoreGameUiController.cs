using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Game;
using Zenject;

namespace EmpireAtWar.Presenters.Game
{
    public class CoreGameUiController : UiController, ICoreGamePresenter, ISkirmishRouteNavigation,
        IUnitActionsViewProvider, ISuperWeaponsViewProvider, ICoreGameHudStatus,
        IObserver<ISelectionSubject>, IObserver<BattleState>, IObserver<GameSpeed>,
        IInitializable, ILateDisposable, ITickable
    {
        private readonly ISelectionService _selectionService;
        private readonly INotifier<BattleState> _battleState;
        private readonly INotifier<GameSpeed> _gameSpeed;
        private readonly ISkirmishFlow _skirmishFlow;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly ICinematicCameraController _cinematicCamera;
        private ICoreGameUi _ui;
        private ISelectionContext _lastSelectionContext;
        private readonly ITooltipService _tooltipService;
        private readonly EmpireAtWar.Services.Input.IInputBindings _bindings;

        private readonly Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>> _routes = new();
        private readonly Dictionary<SkirmishUiRoutePosition, bool> _routeStates = new();
        private EndGamePresenter _endGamePresenter;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        private string _factionName = "";

        private int _stationLevel;

        private BattleState _state;
        private GameSpeed _speed;

        private bool IsBattleEnded => _state == BattleState.Ended;

        public IUnitActionsView UnitActionsView => _ui.UnitActionsView;
        public ISuperWeaponsView SuperWeaponsView => _ui.SuperWeaponsView;

        public CoreGameUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISelectionService selectionService,
            INotifier<BattleState> battleState,
            INotifier<GameSpeed> gameSpeed,
            ISkirmishFlow skirmishFlow,
            INotifier<BattleResult> battleVictoryNotifier,
            ICinematicCameraController cinematicCamera,
            ITooltipService tooltipService,
            EmpireAtWar.Services.Input.IInputBindings bindings) : base(uiService, cancelRouter)
        {
            _selectionService = selectionService;
            _battleState = battleState;
            _gameSpeed = gameSpeed;
            _skirmishFlow = skirmishFlow;
            _battleVictoryNotifier = battleVictoryNotifier;
            _cinematicCamera = cinematicCamera;
            _tooltips = new TooltipRequests(tooltipService);
            _tooltipService = tooltipService;
            _bindings = bindings;
        }

        public void Initialize()
        {
            _ui = (ICoreGameUi)UiService.CreateUi(UiType.CoreGame);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _battleState.AddObserver(this);
            _gameSpeed.AddObserver(this);
            _tooltipHover = new TooltipHoverSubscription(
                ((ITooltipHoverView)_ui).TooltipHover, HandleTooltipHover, _tooltips);
            _endGamePresenter = new EndGamePresenter(
                battleVictoryNotifier: _battleVictoryNotifier,
                view: _ui.PrepareEndGameView(UiService.PopupCanvasTransform),
                returnToMenu: _skirmishFlow.ExitSkirmish, tooltipService: _tooltipService);
            _selectionService.AddObserver(this);

            foreach (KeyValuePair<SkirmishUiRoutePosition, List<ISkirmishUiRoute>>
                     routesAtPosition in _routes)
            {
                for (int i = 0; i < routesAtPosition.Value.Count; i++)
                {
                    ActivateRoute(
                        routesAtPosition.Key,
                        routesAtPosition.Value[i],
                        IsRouteActive(routesAtPosition.Key));
                }
            }

            UpdateContentVisibility(null);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _battleState.RemoveObserver(this);
            _gameSpeed.RemoveObserver(this);
            if (_endGamePresenter != null)
            {
                _endGamePresenter.Dispose();
            }

            if (_ui != null)
            {
                foreach (KeyValuePair<SkirmishUiRoutePosition, List<ISkirmishUiRoute>>
                         routesAtPosition in _routes)
                {
                    for (int i = 0; i < routesAtPosition.Value.Count; i++)
                    {
                        ActivateRoute(
                            routesAtPosition.Key,
                            routesAtPosition.Value[i],
                            false);
                    }
                }
            }

            _routes.Clear();
            if (_ui != null)
            {
                _ui.Dispose();
                _tooltipHover.Dispose();
            }
        }

        public void RegisterRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (!_routes.TryGetValue(position, out List<ISkirmishUiRoute> routes))
            {
                routes = new List<ISkirmishUiRoute>();
                _routes.Add(position, routes);
            }

            if (routes.Contains(route))
            {
                return;
            }

            routes.Add(route);
            if (_ui != null)
            {
                ActivateRoute(position, route, IsRouteActive(position));
            }
        }

        public void UnregisterRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route)
        {
            if (!_routes.TryGetValue(position, out List<ISkirmishUiRoute> routes) ||
                !routes.Contains(route))
            {
                return;
            }

            if (_ui != null)
            {
                ActivateRoute(position, route, false);
            }

            routes.Remove(route);
            if (routes.Count == 0)
            {
                _routes.Remove(position);
            }
        }

        public void SetRouteActive(
            SkirmishUiRoutePosition position,
            bool isActive)
        {
            _routeStates[position] = isActive;

            if (position == SkirmishUiRoutePosition.Content && _ui != null)
            {
                UpdateContentVisibility(_lastSelectionContext);
            }

            if (_ui == null ||
                !_routes.TryGetValue(position, out List<ISkirmishUiRoute> routes))
            {
                return;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                ActivateRoute(position, routes[i], isActive);
            }
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (selectionSubject.UpdatedScope == SelectionScope.Local)
            {
                _lastSelectionContext = selectionSubject.PlayerSelectionContext;
                UpdateContentVisibility(_lastSelectionContext);
            }
        }

        public void UpdateState(BattleState state)
        {
            _state = state;
            _ui.SetTimeControls(_state != BattleState.Running, _speed);
        }

        public void UpdateState(GameSpeed speed)
        {
            _speed = speed;
            _ui.SetTimeControls(_state != BattleState.Running, _speed);
        }

        private void UpdateContentVisibility(ISelectionContext context)
        {
            _ui.SetContentLayout(
                context != null && context.HasSelectable &&
                context.SelectionType == SelectionType.Base,
                context != null && context.HasSelectable &&
                context.SelectionType == SelectionType.Ship && context.Count > 1);

            bool isVisible = IsRouteActive(SkirmishUiRoutePosition.Content) &&
                             context != null && context.HasSelectable &&
                             (context.SelectionType == SelectionType.Base ||
                              context.SelectionType == SelectionType.Ship && HasMovableSelection(context));
            _ui.SetContentVisible(isVisible);
        }

        private static bool HasMovableSelection(ISelectionContext context)
        {
            if (context == null)
            {
                return false;
            }

            for (int i = 0; i < context.Entities.Count; i++)
            {
                if (!context.Entities[i].HealthModel.IsDestroyed &&
                    context.Entities[i].TryGetFacade(out IMoveFacade _))
                {
                    return true;
                }
            }

            return false;
        }

        public void Play()
        {
            _skirmishFlow.TogglePause();
        }

        public void ClearFleetSelection() => _selectionService.RemoveSelectable(_lastSelectionContext);

        public void Tick()
        {
            _ui.SetHudStatus(_factionName, _stationLevel,
                _lastSelectionContext != null ? _lastSelectionContext.Count : 0, IsBattleEnded);
        }

        public void SetProductionStatus(string faction, int level)
        {
            _factionName = faction;
            _stationLevel = level;
        }

        public void SpeedUp()
        {
            _skirmishFlow.ToggleSpeedUp();
        }

        public void ToggleReinforcement()
        {
            if (IsBattleEnded)
            {
                return;
            }

            SkirmishUiRoutePosition position = SkirmishUiRoutePosition.Reinforcement;
            SetRouteActive(position, !IsRouteActive(position));
        }

        public void StartCinematic()
        {
            _cinematicCamera.Enter();
        }

        private void ActivateRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route,
            bool isActive)
        {
            route.Activate(isActive, _ui.GetRouteParent(position));
        }

        private bool IsRouteActive(SkirmishUiRoutePosition position)
        {
            return !_routeStates.TryGetValue(position, out bool isActive) ||
                   isActive;
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => !IsBattleEnded, () =>
                new TooltipContent(title: (string)key, description: (string)key switch
                {
                    "Pause" => "Pause or resume the battle.",
                    "Speed" => "Switch the battle speed.",
                    "Clear fleet" => "Deselect all currently selected units.",
                    "Orbital" => "Open orbital weapons. Construct a charge at the station, then choose a ready weapon and a target.",
                    "Reinforcements" => "Open or close available reinforcements. Drag a card into a friendly deployment zone.",
                    "Cinematic" => $"Enter cinematic camera mode. Exit with {TooltipBindings.Get(_bindings, "Ui", "Cancel")}.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));
    }
}
