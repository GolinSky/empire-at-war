using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Stations;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Finds the place a step marks. Sites are picked fresh each frame, so the marked site follows the lesson
    /// (free site → captured → mine) without remembering ids; the marked unit stays the same while it lives.
    /// </summary>
    public sealed class HintTargets
    {
        /// <summary>Sites farther than this share of the farthest site from the middle count as edge sites.</summary>
        private const float EDGE_FRACTION = 0.5f;
        /// <summary>Central sites lie within this multiple of the nearest mining site's distance to the middle.</summary>
        private const float CENTRAL_ZONE_FACTOR = 2f;
        /// <summary>Deploy marker: this share of the way from the station toward the middle.</summary>
        private const float NEAR_STATION_FRACTION = 0.15f;

        private readonly IStationRegistry _stations;
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly ICaptureSiteDirectory _sites;
        private readonly IPlayerRoster _roster;
        private IEntity _unit;
        private bool _hasLayout;
        private Vector3 _middle;
        private float _centralDistanceSqr;
        private ICaptureSite _foggedSite;

        public HintTargets(IStationRegistry stations, IEntityLocator entities, ILocalPlayer player,
            IPlayerRoster roster, IObjectResolver resolver)
        {
            _roster = roster;
            _stations = stations;
            _entities = entities;
            _player = player;
            _sites = resolver.TryResolve(typeof(ICaptureSiteDirectory), out object sites)
                ? (ICaptureSiteDirectory)sites
                : null;
        }

        /// <param name="trigger">The lesson's trigger, for <see cref="HintTargetId.Trigger"/>.</param>
        public bool TryGet(HintTargetId id, IHintCondition trigger, out Vector3 position)
        {
            position = default;
            switch (id)
            {
                case HintTargetId.None: return false;
                case HintTargetId.Trigger:
                    return trigger is IHintWorldTarget target && target.TryGetWorldTarget(out position);
                case HintTargetId.OwnUnit: return TryGetUnit(out position);
                case HintTargetId.OwnStation: return TryGetStation(out position);
                case HintTargetId.MiningSite:
                case HintTargetId.CentralMiningSite:
                case HintTargetId.EdgeMiningSite:
                case HintTargetId.SatelliteSite: return TryGetSiteToTake(id, out position);
                case HintTargetId.NearOwnStation: return TryGetNearStation(out position);
                case HintTargetId.EnemyStation: return TryGetEnemyStation(out position);
                case HintTargetId.OwnMine: return TryGetOwnMine(out position);
                case HintTargetId.FoggedMiningSite: return TryGetFoggedSite(out position);
                case HintTargetId.NewestUnit:
                    if (!TryGetNewestUnit(out IEntity newest)) return false;
                    position = PositionOf(newest);
                    return true;
                default: throw new System.ArgumentOutOfRangeException(nameof(id), id, "No hint target for this id.");
            }
        }

        private bool TryGetStation(out Vector3 position)
        {
            position = default;
            if (!_stations.TryGetLivingStation(_player.Id, out IEntity station)) return false;
            position = PositionOf(station);
            return true;
        }

        private bool TryGetUnit(out Vector3 position)
        {
            position = default;
            if (_unit == null || _unit.HealthModel.IsDestroyed) _unit = FindUnitNearStation();
            if (_unit == null) return false;
            position = PositionOf(_unit);
            return true;
        }

        private IEntity FindUnitNearStation()
        {
            bool hasStation = TryGetStation(out Vector3 station);
            IEntity best = null;
            float bestDistance = float.MaxValue;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_player.IsLocal(entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IMoveFacade _)) continue;
                float distance = hasStation ? (PositionOf(entity) - station).sqrMagnitude : 0f;
                if (distance >= bestDistance) continue;
                best = entity;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>
        /// Whether a site belongs to a site target: <see cref="HintTargetId.MiningSite"/> (any mining),
        /// <see cref="HintTargetId.CentralMiningSite"/>, <see cref="HintTargetId.EdgeMiningSite"/> or
        /// <see cref="HintTargetId.SatelliteSite"/>.
        /// </summary>
        public bool IsSiteOf(HintTargetId id, ICaptureSite site)
        {
            switch (id)
            {
                case HintTargetId.MiningSite: return site.Kind == CaptureSiteKind.Mining;
                case HintTargetId.CentralMiningSite: return site.Kind == CaptureSiteKind.Mining && IsCentral(site);
                case HintTargetId.EdgeMiningSite: return site.Kind == CaptureSiteKind.Mining && !IsCentral(site);
                case HintTargetId.SatelliteSite: return site.Kind == CaptureSiteKind.Satellite;
                default: throw new System.ArgumentOutOfRangeException(nameof(id), id, "Not a site target.");
            }
        }

        // The central zone is the cluster of mining sites around the middle; every other site is much farther out.
        private bool IsCentral(ICaptureSite site)
        {
            ReadLayout();
            return (site.Center - _middle).sqrMagnitude <= _centralDistanceSqr;
        }

        private void ReadLayout()
        {
            if (_hasLayout) return;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (ICaptureSite site in _sites.Sites)
            {
                sum += site.Center;
                count++;
            }
            _middle = count > 0 ? sum / count : Vector3.zero;
            float nearest = float.MaxValue;
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Mining)
                    nearest = Mathf.Min(nearest, (site.Center - _middle).magnitude);
            float limit = nearest * CENTRAL_ZONE_FACTOR;
            _centralDistanceSqr = limit * limit;
            _hasLayout = true;
        }

        private bool TryGetNearStation(out Vector3 position)
        {
            if (!TryGetStation(out position)) return false;
            if (_sites == null) return true;
            ReadLayout();
            position = Vector3.Lerp(position, new Vector3(_middle.x, position.y, _middle.z), NEAR_STATION_FRACTION);
            return true;
        }

        private bool TryGetEnemyStation(out Vector3 position)
        {
            position = default;
            foreach (PlayerSlot slot in _roster.Players)
            {
                if (!_player.IsHostile(slot.Id) || !_stations.TryGetLivingStation(slot.Id, out IEntity station)) continue;
                position = PositionOf(station);
                return true;
            }
            return false;
        }

        // Sites on our side first: distance to our station minus distance to the nearest hostile station. On a
        // corner map the two expansions equally far from us differ by how close they are to the enemy.
        private float SideScore(ICaptureSite site, Vector3 station)
        {
            float enemyDistance = float.MaxValue;
            foreach (PlayerSlot slot in _roster.Players)
                if (_player.IsHostile(slot.Id) && _stations.TryGetLivingStation(slot.Id, out IEntity enemy))
                    enemyDistance = Mathf.Min(enemyDistance, (site.Center - PositionOf(enemy)).magnitude);
            float ownDistance = (site.Center - station).magnitude;
            return enemyDistance == float.MaxValue ? ownDistance : ownDistance - enemyDistance;
        }

        private bool TryGetSiteToTake(HintTargetId id, out Vector3 position)
        {
            position = default;
            if (_sites == null) return false;
            bool hasStation = TryGetStation(out Vector3 station);
            ICaptureSite best = null;
            float bestDistance = float.MaxValue;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (!IsSiteOf(id, site)) continue;
                bool isOwn = _player.IsLocal(site.Owner);
                if (isOwn && site.State == CaptureSiteState.Owned)
                {
                    // Captured but not built yet: the lesson goes on with this site.
                    position = site.Center;
                    return true;
                }
                if (isOwn) continue;
                float distance = hasStation ? SideScore(site, station) : 0f;
                if (distance >= bestDistance) continue;
                best = site;
                bestDistance = distance;
            }
            if (best == null) return false;
            position = best.Center;
            return true;
        }

        // Picked once and kept, so the mark does not jump to another site when the scout reveals it.
        private bool TryGetFoggedSite(out Vector3 position)
        {
            position = default;
            if (_sites == null) return false;
            if (_foggedSite == null || _player.IsLocal(_foggedSite.Owner)) _foggedSite = FindEdgeSite();
            if (_foggedSite == null) return false;
            position = _foggedSite.Center;
            return true;
        }

        // Free mining sites in the outer half (by distance from the middle of all sites) are side expansions;
        // the nearest to the station wins. The contested center is never picked.
        private ICaptureSite FindEdgeSite()
        {
            Vector3 middle = Vector3.zero;
            int count = 0;
            foreach (ICaptureSite site in _sites.Sites)
            {
                middle += site.Center;
                count++;
            }
            if (count == 0) return null;
            middle /= count;
            float farthest = 0f;
            foreach (ICaptureSite site in _sites.Sites)
                farthest = Mathf.Max(farthest, (site.Center - middle).sqrMagnitude);
            float edge = farthest * EDGE_FRACTION * EDGE_FRACTION;
            bool hasStation = TryGetStation(out Vector3 station);
            ICaptureSite best = null;
            float bestDistance = float.MaxValue;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != CaptureSiteKind.Mining || _player.IsLocal(site.Owner) ||
                    (site.Center - middle).sqrMagnitude < edge) continue;
                float distance = hasStation ? (site.Center - station).sqrMagnitude : 0f;
                if (distance >= bestDistance) continue;
                best = site;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>The local movable unit that arrived last; entity ids grow with each spawn.</summary>
        public bool TryGetNewestUnit(out IEntity newest)
        {
            newest = null;
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IMoveFacade _) && (newest == null || entity.Id > newest.Id))
                    newest = entity;
            return newest != null;
        }

        private bool TryGetOwnMine(out Vector3 position)
        {
            position = default;
            if (_sites == null) return false;
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Mining && _player.IsLocal(site.Owner) &&
                    site.State != CaptureSiteState.Neutral)
                {
                    position = site.Center;
                    return true;
                }
            return false;
        }

        private static Vector3 PositionOf(IEntity entity) =>
            entity.GetFacade<IEntityTransformFacade>().Transform.position;
    }
}
