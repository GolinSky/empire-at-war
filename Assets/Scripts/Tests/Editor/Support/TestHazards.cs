using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Services.Hazards;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    internal sealed class TestHazards : IHazardService
    {
        public event Action<IEntity, SpaceHazardData> Entered { add { } remove { } }
        public IReadOnlyList<IHazardSource> Sources { get; set; } = Array.Empty<IHazardSource>();
        public bool Blocks { get; set; }
        public bool BlocksReinforcement(Vector3 position) => Blocks;
        public bool TryGetHazardAt(Vector3 position, out SpaceHazardData data)
        {
            data = null;
            return false;
        }
        public float GetPathCost(Vector3 position) => 0f;

        /// <summary>A smooth single-lobe cloud; <paramref name="length"/> is the semi-axis along <paramref name="direction"/>.</summary>
        public static HazardShape CreateEllipse(SpaceHazardData data, Vector3 center, Vector3 direction,
            float length, float width, float cellSize = 2f)
        {
            return new HazardShape(new HazardSpot(data, new[] { new HazardLobe(center, direction, length, width) },
                new HazardFrame(center, direction, false, Vector2.zero), 0f, cellSize));
        }
    }
}
