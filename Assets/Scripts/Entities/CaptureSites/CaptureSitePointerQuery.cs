using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public static class CaptureSitePointerQuery
    {
        public static T Find<T>(IReadOnlyList<T> sites, Ray ray, CaptureSiteKind? kind = null) where T : class, ICaptureSite
        {
            T nearestSite = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < sites.Count; i++)
            {
                T site = sites[i];
                if (!site.IsRevealed || (kind.HasValue && site.Kind != kind.Value)) continue;
                float distance = site.PointerHitDistance(ray);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestSite = site;
            }
            return nearestSite;
        }
    }
}
