using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Game;
using Zenject;

namespace EmpireAtWar.Presenters.Game
{
    public class CoreGameUiController : ICoreGamePresenter, ISkirmishRouteNavigation,
        IUnitActionsViewProvider, ISuperWeaponsViewProvider,
        IObserver<ISelectionSubject>, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ISelectionService _selectionService;
        private readonly ISkirmishSessionModelObserver _sessionModel;
        private readonly ISkirmishFlow _skirmishFlow;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly ICinematicCameraController _cinematicCamera;
        private readonly Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>> _routes = new();
        private readonly Dictionary<SkirmishUiRoutePosition, bool> _routeStates = new();

        private ICoreGameUi _ui;
        private EndGamePresenter _endGamePresenter;
        private ISelectionContext _lastSelectionContext;

        public IUnitActionsView UnitActionsView => _ui.UnitActionsView;
        public ISuperWeaponsView SuperWeaponsView => _ui.SuperWeaponsView;

        public CoreGameUiController(
            IUiService uiService,
            ISelectionService selectionService,
            ISkirmishSessionModelObserver sessionModel,
            ISkirmishFlow skirmishFlow,
            INotifier<BattleResult> battleVictoryNotifier,
            ICinematicCameraController cinematicCamera)
        {
            _uiService = uiService;
            _selectionService = selectionService;
            _sessionModel = sessionModel;
            _skirmishFlow = skirmishFlow;
            _battleVictoryNotifier = battleVictoryNotifier;
            _cinematicCamera = cinematicCamera;
        }

        public void Initialize()
        {
            _ui = (ICoreGameUi)_uiService.CreateUi(UiType.CoreGame);
            _ui.SetModel(_sessionModel);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _endGamePresenter = new EndGamePresenter(
                _battleVictoryNotifier,
                _ui.PrepareEndGameView(_uiService.PopupCanvasTransform),
                _skirmishFlow.ExitSkirmish);
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
            if (selectionSubject.UpdatedType == PlayerType.Player)
            {
                _lastSelectionContext = selectionSubject.PlayerSelectionContext;
                UpdateContentVisibility(_lastSelectionContext);
            }
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

        public void SpeedUp()
        {
            _skirmishFlow.ToggleSpeedUp();
        }

        public void ToggleReinforcement()
        {
            if (_sessionModel.IsBattleEnded)
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
    }
}
