using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.Vision
{
    /// <summary>
    /// Every player's vision, under one rule: a point is visible to a viewer when it lies inside the
    /// radius of a source owned by that viewer's team. Distances ignore height (XZ plane only).
    /// </summary>
    public interface IVisionService
    {
        IReadOnlyList<OwnedCircle> Sources { get; }

        /// <summary>Adds a source, or updates the radius of one already registered.</summary>
        void Register(PlayerId owner, Transform source, float radius);

        void Unregister(Transform source);

        bool IsVisible(PlayerId viewer, Vector3 position);

        /// <summary>True when any part of the XZ circle around the position is visible to the viewer.</summary>
        bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius);
    }
}
