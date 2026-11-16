using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Runs one <see cref="HostileIntelModel"/> gather pass per active AI team instead of once per AI player,
    /// since <see cref="IVisionService"/> is already ally-shared.
    /// </summary>
    public sealed class TeamHostileIntelService : IInitializable, ITickable, ILateDisposable
    {
        private const float SCAN_INTERVAL = 1f;

        private sealed class TeamEntry
        {
            public readonly TeamId Team;
            public readonly HostileIntelModel Intel;
            public readonly List<PlayerId> Members = new List<PlayerId>();

            public TeamEntry(TeamId team, HostileIntelModel intel)
            {
                Team = team;
                Intel = intel;
            }
        }

        private readonly IEntityLocator _entityLocator;
        private readonly IVisionService _vision;
        private readonly IPlayerRoster _playerRoster;
        private readonly TeamIntelRegistry _intelRegistry;
        private readonly AiWorldSnapshot _worldSnapshot;
        private readonly Dictionary<TeamId, TeamEntry> _teams = new Dictionary<TeamId, TeamEntry>();

        private float _nextScanTime;

        public TeamHostileIntelService(
            IEntityLocator entityLocator,
            IVisionService vision,
            IPlayerRoster playerRoster,
            TeamIntelRegistry intelRegistry,
            AiWorldSnapshot worldSnapshot)
        {
            _entityLocator = entityLocator;
            _vision = vision;
            _playerRoster = playerRoster;
            _intelRegistry = intelRegistry;
            _worldSnapshot = worldSnapshot;
        }

        public void Initialize()
        {
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            _teams.Clear();
        }

        public void RegisterPlayer(PlayerSlot owner)
        {
            if (!_teams.TryGetValue(owner.Team, out TeamEntry entry))
            {
                entry = new TeamEntry(owner.Team, _intelRegistry.Get(owner.Team));
                _teams.Add(owner.Team, entry);
            }

            if (!entry.Members.Contains(owner.Id))
            {
                entry.Members.Add(owner.Id);
            }
        }

        public void UnregisterPlayer(PlayerSlot owner)
        {
            if (!_teams.TryGetValue(owner.Team, out TeamEntry entry))
            {
                return;
            }

            entry.Members.Remove(owner.Id);
            if (entry.Members.Count == 0)
            {
                _teams.Remove(owner.Team);
            }
        }

        public void Tick()
        {
            if (_teams.Count == 0)
            {
                return;
            }

            float now = Time.time;
            if (now < _nextScanTime)
            {
                return;
            }

            _nextScanTime = now + SCAN_INTERVAL;
            GatherAll(now);
        }

        public void GatherAll(float now)
        {
            if (_teams.Count == 0)
            {
                return;
            }

            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            foreach (TeamEntry team in _teams.Values)
            {
                if (team.Members.Count == 0)
                {
                    continue;
                }

                PlayerId viewer = team.Members[0];
                team.Intel.ForgetExpired(now);
                for (int i = 0; i < units.Count; i++)
                {
                    AiWorldSnapshot.Entry entry = units[i];
                    if (!_playerRoster.IsHostile(viewer, entry.Owner) ||
                        entry.Entity.HealthModel.IsDestroyed ||
                        entry.Entity.IsCloaked())
                    {
                        continue;
                    }

                    if (!_vision.IsVisible(viewer, entry.Entity))
                    {
                        continue;
                    }

                    team.Intel.Report(new HostileSighting(
                        entry.Id,
                        entry.UnitTypeId,
                        entry.Owner,
                        ToPoint(entry.Position),
                        entry.Hull,
                        entry.Shields,
                        entry.OffenseScale,
                        now));
                }
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            foreach (TeamEntry team in _teams.Values)
            {
                team.Intel.Forget(entity.Id);
            }
        }
    }
}
