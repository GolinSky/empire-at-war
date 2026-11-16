using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ReinforcementZones;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Battle-scope registry of <see cref="TeamAiPicture"/> instances, one per active AI team.</summary>
    public sealed class TeamAiPictureRegistry
    {
        private readonly TeamIntelRegistry _intelRegistry;
        private readonly IPlayerRoster _roster;
        private readonly IReinforcementZoneSource _zones;
        private readonly IReadOnlyList<IEnemyCaptureObjectiveSource> _captureSources;
        private readonly UnitCombatProfileCatalog _catalog;
        private readonly AiWorldSnapshot _worldSnapshot;
        private readonly Dictionary<TeamId, TeamAiPicture> _pictures = new Dictionary<TeamId, TeamAiPicture>();

        public TeamAiPictureRegistry(
            TeamIntelRegistry intelRegistry,
            IPlayerRoster roster,
            IReinforcementZoneSource zones,
            IReadOnlyList<IEnemyCaptureObjectiveSource> captureSources,
            UnitCombatProfileCatalog catalog,
            AiWorldSnapshot worldSnapshot)
        {
            _intelRegistry = intelRegistry;
            _roster = roster;
            _zones = zones;
            _captureSources = captureSources;
            _catalog = catalog;
            _worldSnapshot = worldSnapshot;
        }

        public TeamAiPicture GetOrCreate(PlayerSlot owner)
        {
            if (!_pictures.TryGetValue(owner.Team, out TeamAiPicture picture))
            {
                picture = new TeamAiPicture(
                    owner.Team,
                    owner.Id,
                    _intelRegistry.Get(owner.Team),
                    _roster,
                    _zones,
                    _captureSources,
                    _catalog,
                    _worldSnapshot);
                _pictures.Add(owner.Team, picture);
            }
            return picture;
        }

        public bool TryGet(TeamId team, out TeamAiPicture picture) =>
            _pictures.TryGetValue(team, out picture);

        public void RefreshAll(float now)
        {
            foreach (TeamAiPicture picture in _pictures.Values)
            {
                picture.Refresh(now);
            }
        }

        public void Clear() => _pictures.Clear();
    }
}
