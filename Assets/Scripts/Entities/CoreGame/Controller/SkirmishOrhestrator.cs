using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Commands.SkirmishGame;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Game;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Game
{
    public class SkirmishOrhestrator : Controller<CoreGameModel>, ICoreGameCommand,
        IObserver<UserNotifierState>, IInitializable, ILateDisposable,
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
        private readonly FactionType _playerFactionType;
        private readonly Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>> _routes =
            new Dictionary<SkirmishUiRoutePosition, List<ISkirmishUiRoute>>();
        private readonly Dictionary<SkirmishUiRoutePosition, bool> _routeStates =
            new Dictionary<SkirmishUiRoutePosition, bool>();

        private CoreGameUi _coreGameUi;
        private GameTimeMode _gameTimeMode;

        public SkirmishOrhestrator(
            CoreGameModel model,
            LazyInject<IUserStateNotifier> userStateNotifier,
            IGameCommand gameCommand,
            IUiService uiService,
            ICameraService cameraService,
            IMapModelObserver mapModel,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType) : base(model)
        {
            _userStateNotifier = userStateNotifier;
            _gameCommand = gameCommand;
            _uiService = uiService;
            _cameraService = cameraService;
            _mapModel = mapModel;
            _playerFactionType = playerFactionType;
            _gameTimeMode = GameTimeMode.Common;
            ChangeTime(_gameTimeMode);
        }

        public void Initialize()
        {
            _userStateNotifier.Value.AddObserver(this);
            _cameraService.MoveTo(
                _mapModel.GetStationPosition(_playerFactionType));
            BaseUi ui = _uiService.CreateUi(UiType.CoreGame);
            _coreGameUi = ui as CoreGameUi
                ?? throw new InvalidOperationException(
                    "The core game prefab does not contain CoreGameUi.");

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
        }

        public void LateDispose()
        {
            _userStateNotifier.Value.RemoveObserver(this);

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

        public void Play()
        {
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
            SkirmishUiRoutePosition position =
                SkirmishUiRoutePosition.Reinforcement;
            SetRouteActive(position, !IsRouteActive(position));
        }

        public void UpdateState(UserNotifierState notifierState)
        {
            if (notifierState == UserNotifierState.ExitGame)
            {
                ChangeTime(GameTimeMode.Common);
                _gameCommand.ExitGame();
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
