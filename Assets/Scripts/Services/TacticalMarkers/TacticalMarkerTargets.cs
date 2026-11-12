using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.TacticalMarkers
{
    public sealed class TacticalMarkerTargets
    {
        private readonly IEntityLocator _entities;
        private readonly IReadOnlyList<ICaptureSiteDirectory> _sites;
        private readonly IVisionService _vision;

        public TacticalMarkerTargets(IEntityLocator entities, IReadOnlyList<ICaptureSiteDirectory> sites, IVisionService vision)
        {
            _entities = entities;
            _sites = sites;
            _vision = vision;
        }

        public ICaptureSite FindSite(long targetId)
        {
            if (!TacticalMarkerTarget.IsSite(targetId)) return null;
            int targetIndex = TacticalMarkerTarget.SiteIndex(targetId);
            int index = 0;
            foreach (ICaptureSiteDirectory directory in _sites)
                foreach (ICaptureSite site in directory.Sites)
                    if (index++ == targetIndex) return site;
            return null;
        }

        public long GetSiteId(ICaptureSite target)
        {
            int index = 0;
            foreach (ICaptureSiteDirectory directory in _sites)
                foreach (ICaptureSite site in directory.Sites)
                {
                    if (ReferenceEquals(site, target)) return TacticalMarkerTarget.SiteId(index);
                    index++;
                }
            throw new KeyNotFoundException("The marker target is not in the battle capture-site directory.");
        }

        public bool TryGetVisiblePoint(PlayerId sender, long targetId, out Vector3 point, out bool exists)
        {
            point = default;
            exists = true;
            ICaptureSite site = FindSite(targetId);
            if (site != null) point = site.Center;
            else if (targetId >= 0 && _entities.TryGetEntity(targetId, out IEntity entity) &&
                     !entity.HealthModel.IsDestroyed &&
                     entity.TryGetFacade(out IEntityTransformFacade transform))
                point = transform.Transform.position;
            else { exists = false; return false; }
            return _vision.IsVisible(sender, point);
        }
    }
}
