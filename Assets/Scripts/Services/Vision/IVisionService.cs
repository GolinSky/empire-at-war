using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.Vision
{
    /// <summary>
    /// Allied vision circles reveal points and intersecting ship hulls in the XZ plane.
    /// A firing reveal additionally exposes one attacker to the attacked player's team.
    /// </summary>
    public interface IVisionService
    {
        event Action<PlayerId, long, bool> RevealChanged;

        bool FogEnabled { get; }

        float FiringRevealSeconds { get; }

        IReadOnlyList<OwnedCircle> Sources { get; }

        /// <summary>Adds a source, or updates the radius of one already registered.</summary>
        void Register(PlayerId owner, Transform source, float radius);

        void Unregister(Transform source);

        bool IsVisible(PlayerId viewer, Vector3 position);

        bool IsVisible(PlayerId viewer, IVisionFootprintFacade footprint);

        bool IsRevealed(PlayerId viewer, long entityId);

        /// <summary>Reveals an attacker to the attacked player's team for a scaled-time duration.</summary>
        void Reveal(PlayerId viewer, long entityId, float duration);

        /// <summary>True when any part of the XZ circle around the position is visible to the viewer.</summary>
        bool IsAreaVisible(PlayerId viewer, Vector3 position, float radius);
    }
}
