using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>
    /// Main menu's <see cref="IGameCommand"/>: while hosting a lobby, Start on the Skirmish/Survival setup screens
    /// starts the online match (all PCs load) instead of an offline battle.
    /// </summary>
    public sealed class OnlineGameCommand : IGameCommand
    {
        private readonly GameController _offline;
        private readonly OnlineMatchService _online;

        public OnlineGameCommand(GameController offline, OnlineMatchService online)
        {
            _offline = offline;
            _online = online;
        }

        public void StartBattle(BattleDefinition definition)
        {
            if (_online.Role != OnlineRole.Host)
            {
                _offline.StartBattle(definition);
                return;
            }

            if (!_online.TryStartMatch(definition))
                Debug.LogWarning("[Online] Cannot start: every player must be ready (or Force Start) and the line-up " +
                    "needs one Human slot per lobby member.");
        }

        public void LoadBattle(BattleSave save) => _offline.LoadBattle(save);

        public void ExitGame() => _offline.ExitGame();
    }
}
