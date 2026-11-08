using EmpireAtWar.Commands.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Client: the host left mid-battle; return to the menu, where the online panel shows the error.</summary>
    public sealed class HostLeftBattleExit : IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly IGameCommand _gameCommand;

        public HostLeftBattleExit(OnlineMatchService online, IGameCommand gameCommand)
        {
            _online = online;
            _gameCommand = gameCommand;
        }

        public void Initialize() => _online.HostLeft += Exit;

        public void LateDispose() => _online.HostLeft -= Exit;

        private void Exit()
        {
            Time.timeScale = 1f;
            _gameCommand.ExitGame();
        }
    }
}
