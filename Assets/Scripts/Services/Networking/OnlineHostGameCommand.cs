using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Saves;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Online host's battle <see cref="IGameCommand"/>: restarting the battle restarts the match on every PC.
    /// Leaving and loading go to the regular command.
    /// </summary>
    public sealed class OnlineHostGameCommand : IGameCommand
    {
        private readonly GameController _offline;
        private readonly OnlineMatchService _online;

        public OnlineHostGameCommand(GameController offline, OnlineMatchService online)
        {
            _offline = offline;
            _online = online;
        }

        public void StartBattle(BattleDefinition definition) => _online.RestartMatch(definition);

        public void LoadBattle(BattleSave save) => _offline.LoadBattle(save);

        public void ExitGame() => _offline.ExitGame();
    }
}
