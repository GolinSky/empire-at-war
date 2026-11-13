using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    // One snapshot per hazard set and map bounds, shared by every clearance/height layer.
    internal sealed class NavigationHazardRaster : IDisposable
    {
        private const float MINIMUM_CELL_SIZE = 4f;
        private const int MAXIMUM_CELLS_PER_AXIS = 160;
        private readonly IHazardSource[] _sources;
        private NativeArray<float> _costs;
        private NativeList<float2> _boundary;

        public Vector2 Min { get; }
        public Vector2 Max { get; }
        public NavigationGridCells Cells { get; }
        public NativeArray<float> Costs => _costs;
        public NativeArray<float2> Boundary => _boundary.AsArray();
        public bool HasExtraCost { get; }

        public NavigationHazardRaster(Vector2Range bounds, IReadOnlyList<IHazardSource> sources)
        {
            Min = bounds.Min;
            Max = bounds.Max;
            Vector2 size = Max - Min;
            float cell = Mathf.Max(MINIMUM_CELL_SIZE, Mathf.Max(size.x, size.y) / MAXIMUM_CELLS_PER_AXIS);
            Cells = new NavigationGridCells(new float2(Min.x, Min.y), cell,
                Mathf.FloorToInt(size.x / cell) + 1, Mathf.FloorToInt(size.y / cell) + 1);
            _sources = new IHazardSource[sources.Count];
            for (int i = 0; i < sources.Count; i++) _sources[i] = sources[i];
            try
            {
                _costs = new NativeArray<float>(sources.Count == 0 ? 0 : Cells.Count, Allocator.Persistent);
                _boundary = new NativeList<float2>(Allocator.Persistent);
                for (int i = 0; i < _costs.Length; i++)
                {
                    float2 p = Cells.GetNodePosition(i);
                    var position = new Vector3(p.x, 0f, p.y);
                    float cost = 0f;
                    foreach (IHazardSource source in _sources)
                        if (source.Contains(position, 0f, false)) cost += source.Data.PathCost;
                    _costs[i] = cost;
                    if (cost > 0f) HasExtraCost = true;
                }
                if (HasExtraCost)
                    new NavMeshHazardBoundaryJob { Cells = Cells, Costs = _costs, Points = _boundary }.Schedule().Complete();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool Matches(Vector2Range bounds, IReadOnlyList<IHazardSource> sources)
        {
            if (!Min.Equals(bounds.Min) || !Max.Equals(bounds.Max) || _sources.Length != sources.Count)
                return false;
            for (int i = 0; i < sources.Count; i++)
                if (!ReferenceEquals(_sources[i], sources[i])) return false;
            return true;
        }

        public bool IsUnpenalizedSegment(Vector3 from, Vector3 to)
        {
            if (!HasExtraCost) return true;
            var cost = new NavigationTravelCost(Cells, _costs);
            float2 start = new float2(from.x, from.z);
            float2 end = new float2(to.x, to.z);
            return cost.Segment(start, end) <= math.distance(start, end) + 0.01f;
        }

        // Compares only the hazard surcharge: a smooth curve is longer than its straight legs by design,
        // and counting that length rejected every turning curve on hazard maps (ships turned in place instead).
        public void Dispose()
        {
            if (_boundary.IsCreated) _boundary.Dispose();
            if (_costs.IsCreated) _costs.Dispose();
        }
    }
}
