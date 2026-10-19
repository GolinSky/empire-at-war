using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Battle-scope entry point for player state: player-scope sources register here, since the battle scope
    /// cannot see into player containers. Players without a source are not saved.
    /// </summary>
    public sealed class PlayerSaveRegistry : IBattleSaveSource
    {
        private readonly SortedDictionary<int, List<IPlayerSaveSource>> _sources =
            new SortedDictionary<int, List<IPlayerSaveSource>>();

        public void Register(PlayerId player, IPlayerSaveSource source)
        {
            if (!_sources.TryGetValue(player.Index, out List<IPlayerSaveSource> sources))
                _sources.Add(player.Index, sources = new List<IPlayerSaveSource>());
            sources.Add(source);
        }

        public void Unregister(PlayerId player, IPlayerSaveSource source)
        {
            List<IPlayerSaveSource> sources = _sources[player.Index];
            sources.Remove(source);
            if (sources.Count == 0) _sources.Remove(player.Index);
        }

        public void Capture(BattleSave save)
        {
            save.Players = _sources.Select(entry =>
            {
                PlayerSave player = PlayerSave.Create(new PlayerId(entry.Key));
                foreach (IPlayerSaveSource source in entry.Value) source.Capture(player);
                return player;
            }).ToArray();
        }

        public void RestoreBeforeUnits(BattleSave save)
        {
            foreach (PlayerSave player in save.Players)
            foreach (IPlayerSaveSource source in _sources[player.Player.Index])
                source.RestoreBeforeUnits(player);
        }

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
            foreach (PlayerSave player in context.Save.Players)
            foreach (IPlayerSaveSource source in _sources[player.Player.Index])
                source.RestoreAfterUnits(player, context);
        }
    }
}
