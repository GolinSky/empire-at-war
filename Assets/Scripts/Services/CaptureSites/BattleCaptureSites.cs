using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>Queries the capture-site modules installed in this battle.</summary>
    public sealed class BattleCaptureSites : ICaptureSitesSystem
    {
        private readonly IReadOnlyList<CaptureSitesSystem> _systems;
        public BattleCaptureSites(IReadOnlyList<CaptureSitesSystem> systems) => _systems = systems;
        public IReadOnlyList<ICaptureSite> Sites => _systems.Count == 0
            ? System.Array.Empty<ICaptureSite>() : _systems[0].Sites;
        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) =>
            _systems.Any(system => system.IsPositionInAnySite(position, clearance));
        public bool TryBuildOnOwnedSite(PlayerId owner) =>
            _systems.Any(system => system.TryBuildOnOwnedSite(owner));

        public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
        {
            foreach (var system in _systems)
                if (system.TryGetCaptureTarget(owner, origin, out position)) return true;
            position = default;
            return false;
        }

        public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
        {
            foreach (var system in _systems)
                if (system.TryGetThreatenedSite(owner, out position)) return true;
            position = default;
            return false;
        }

        public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
        {
            foreach (var system in _systems)
                if (system.TryGetRaidTarget(attacker, origin, out position)) return true;
            position = default;
            return false;
        }
    }
}
