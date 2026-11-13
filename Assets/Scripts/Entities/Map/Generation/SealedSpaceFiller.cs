using System;
using System.Collections.Generic;
using EmpireAtWar.Utils;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Turns open space that the finished fields wall off from every station into asteroid field,
    /// so no reinforcement point or order can land a ship somewhere it cannot leave.
    /// </summary>
    public sealed class SealedSpaceFiller
    {
        private readonly MapGenerationSettings _settings;
        private readonly RockScatter _rockScatter;

        public SealedSpaceFiller(MapGenerationSettings settings)
        {
            _settings = settings;
            _rockScatter = new RockScatter(settings);
        }

        public void Fill(MapSizeSettings size, IReadOnlyList<MapStation> stations, List<AsteroidField> fields,
            Random random)
        {
            FieldGrid blocked = new FieldGrid(size.Bounds, size.FieldCellSize);
            for (int index = 0; index < blocked.Count; index++)
                blocked.SetField(index, IsCovered(blocked.GetCenter(index), fields));

            List<int> starts = new List<int>(stations.Count);
            foreach (MapStation station in stations)
                starts.Add(blocked.GetIndex(station.Position));
            List<int> sealedCells = blocked.FindSealedOpenCells(starts);
            if (sealedCells.Count == 0) return;

            // Measured alone, each sealed area's edge distance is its own inscribed size, so the cover stays inside it.
            FieldGrid grid = new FieldGrid(size.Bounds, size.FieldCellSize);
            foreach (int cell in sealedCells)
                if (!size.IsArchetypeLayout || !ArchetypeFieldPattern.IsGuaranteedGap(grid.GetCenter(cell), size))
                    grid.SetField(cell, true);
            int[] edgeDistances = grid.GetEdgeDistances();
            foreach (List<int> cells in grid.FindFields())
            {
                fields.Add(new AsteroidField(
                    FieldVolumeCover.Cover(grid, cells, edgeDistances, Array.Empty<MapLane>(), 0f,
                        requireCellCenterCoverage: true),
                    _rockScatter.Scatter(grid, cells, size.RockDensity, random),
                    _settings.FieldFloor,
                    _settings.FieldCeiling));
            }
        }

        private static bool IsCovered(UnityEngine.Vector3 position, IReadOnlyList<AsteroidField> fields)
        {
            foreach (AsteroidField field in fields)
            foreach (FieldVolume volume in field.Volumes)
                if (PlanarGeometry.Distance(position, volume.Center) < volume.Radius)
                    return true;
            return false;
        }
    }
}
