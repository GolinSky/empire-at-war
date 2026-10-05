using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.OwnedAreas
{
    /// <summary>Circles keyed by their transform. Registering a transform again updates its owner and radius.</summary>
    public sealed class OwnedCircleSet
    {
        private readonly List<OwnedCircle> _circles = new List<OwnedCircle>();

        public IReadOnlyList<OwnedCircle> Circles => _circles;

        public void Register(PlayerId owner, Transform source, float radius)
        {
            int index = IndexOf(source);
            if (index >= 0)
            {
                _circles[index].Owner = owner;
                _circles[index].Radius = radius;
                return;
            }

            _circles.Add(new OwnedCircle(owner, source, radius));
        }

        public void Unregister(Transform source)
        {
            int index = IndexOf(source);
            if (index >= 0)
            {
                _circles.RemoveAt(index);
            }
        }

        /// <summary>True when a circle whose owner is (or, with <paramref name="allied"/> false, is not)
        /// allied with <paramref name="team"/> reaches within <paramref name="margin"/> of the position.</summary>
        public bool AnyContains(Vector3 position, float margin, IPlayerRelations relations, PlayerId team, bool allied)
        {
            foreach (OwnedCircle circle in _circles)
            {
                if (relations.IsAllied(circle.Owner, team) == allied && circle.Contains(position, margin)) return true;
            }

            return false;
        }

        private int IndexOf(Transform source)
        {
            for (int i = 0; i < _circles.Count; i++)
            {
                if (_circles[i].Transform == source) return i;
            }

            return -1;
        }
    }
}
