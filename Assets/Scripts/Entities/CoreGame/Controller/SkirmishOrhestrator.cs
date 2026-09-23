using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Game;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public class SkirmishOrhestrator : Controller<CoreGameData>, ICoreGameCommand,
        IObserver<UserNotifierState>, IObserver<ISelectionSubject>, IObserver<BattleResult>, IInitializable, ILateDisposable,
        ISkirmishRouteNavigation
    {
        private const float SPEED_UP_TIME_SCALE = 4f;
        private const float DEFAULT_TIME_SCALE = 1f;
        private const float PAUSE_TIME_SCALE = 0f;

        private readonly LazyInject<IUserStateNotifier> _userStateNotifier;
        private readonly IGameCommand _gameCommand;
        private readonly IUiService _uiService;
        private readonly ICameraService _cameraService;
        private readonly IMapModelObserver _mapModel;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly FactionType _playerFactionType;
        private readonly ISelectionService _selectionService;
        private readonly Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>> _routes =
            new Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>>();
        private readonly Dictionary<SkirmishUiRoutePosition, bool> _routeStates =
            new Dictionary<SkirmishUiRoutePosition, bool>();

        private CoreGameUi _coreGameUi;
        private GameTimeMode _gameTimeMode;
        private EndGamePresenter _endGamePresenter;
        private ISelectionContext _lastSelectionContext;
        private bool _hasBattleEnded;

        public SkirmishOrhestrator(
            CoreGameData model,
            LazyInject<IUserStateNotifier> userStateNotifier,
            IGameCommand gameCommand,
            IUiService uiService,
            ICameraService cameraService,
            IMapModelObserver mapModel,
            INotifier<BattleResult> battleVictoryNotifier,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType,
            ISelectionService selectionService) : base(model)
        {
            _userStateNotifier = userStateNotifier;
            _gameCommand = gameCommand;
            _uiService = uiService;
            _cameraService = cameraService;
            _mapModel = mapModel;
            _battleVictoryNotifier = battleVictoryNotifier;
            _playerFactionType = playerFactionType;
            _selectionService = selectionService;
            _gameTimeMode = GameTimeMode.Common;
            ChangeTime(_gameTimeMode);
        }

        public void Initialize()
        {
            _userStateNotifier.Value.AddObserver(this);
            _selectionService.AddObserver(this);
            _cameraService.MoveTo(
                _mapModel.GetStationPosition(_playerFactionType));
            BaseUi ui = _uiService.CreateUi(UiType.CoreGame);
            _coreGameUi = ui as CoreGameUi
                ?? throw new InvalidOperationException(
                    "The core game prefab does not contain CoreGameUi.");
            _battleVictoryNotifier.AddObserver(this);
            _endGamePresenter = new EndGamePresenter(
                _battleVictoryNotifier,
                _coreGameUi.PrepareEndGameView(_uiService.PopupCanvasTransform),
                ExitSkirmish);

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
            _userStateNotifier.Value.RemoveObserver(this);
            _selectionService.RemoveObserver(this);
            _battleVictoryNotifier.RemoveObserver(this);
            if (_endGamePresenter != null)
            {
                _endGamePresenter.Dispose();
            }

            if (_coreGameUi != null)
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

            if (_coreGameUi != null)
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

            if (_coreGameUi != null)
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

            if (position == SkirmishUiRoutePosition.Content)
            {
                UpdateContentVisibility(_lastSelectionContext);
            }

            if (_coreGameUi == null ||
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
            if (!IsRouteActive(SkirmishUiRoutePosition.Content))
            {
                Model.IsContentVisible = false;
                return;
            }

            if (context == null || !context.HasSelectable)
            {
                Model.IsContentVisible = false;
                return;
            }

            if (context.SelectionType == SelectionType.Base)
            {
                Model.IsContentVisible = true;
                return;
            }

            if (context.SelectionType == SelectionType.Ship && HasMovableSelection(context))
            {
                Model.IsContentVisible = true;
                return;
            }

            Model.IsContentVisible = false;
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
                    context.Entities[i].TryGetCommand(out IMoveCommand _))
                {
                    return true;
                }
            }

            return false;
        }

        public void Play()
        {
            if (_hasBattleEnded)
            {
                return;
            }

            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                    _gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Pause;
                    break;
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
            }

            ChangeTime(_gameTimeMode);
        }

        public void SpeedUp()
        {
            if (_hasBattleEnded)
            {
                return;
            }

            switch (_gameTimeMode)
            {
                case GameTimeMode.Common:
                    _gameTimeMode = GameTimeMode.SpeedUp;
                    break;
                case GameTimeMode.SpeedUp:
                    _gameTimeMode = GameTimeMode.Common;
                    break;
                case GameTimeMode.Pause:
                    _gameTimeMode = GameTimeMode.SpeedUp;
                    break;
            }

            ChangeTime(_gameTimeMode);
        }

        public void ToggleReinforcement()
        {
            if (_hasBattleEnded)
            {
                return;
            }

            SkirmishUiRoutePosition position =
                SkirmishUiRoutePosition.Reinforcement;
            SetRouteActive(position, !IsRouteActive(position));
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ExitSkirmish();
                return;
            }

            if (_hasBattleEnded)
            {
                return;
            }

            ChangeTime(
                notifierState == UserNotifierState.InMenu
                    ? GameTimeMode.Pause
                    : GameTimeMode.Common);
        }

        private void ChangeTime(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Common:
                    Time.timeScale = DEFAULT_TIME_SCALE;
                    break;
                case GameTimeMode.SpeedUp:
                    Time.timeScale = SPEED_UP_TIME_SCALE;
                    break;
                case GameTimeMode.Pause:
                    Time.timeScale = PAUSE_TIME_SCALE;
                    break;
            }

            Model.GameTimeMode = mode;
        }

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
            ChangeTime(GameTimeMode.Pause);
        }

        private void ExitSkirmish()
        {
            ChangeTime(GameTimeMode.Common);
            _gameCommand.ExitGame();
        }

        private void ActivateRoute(
            SkirmishUiRoutePosition position,
            ISkirmishUiRoute route,
            bool isActive)
        {
            route.Activate(isActive, _coreGameUi.GetRouteParent(position));
        }

        private bool IsRouteActive(SkirmishUiRoutePosition position)
        {
            return !_routeStates.TryGetValue(position, out bool isActive) ||
                   isActive;
        }
    }
}
