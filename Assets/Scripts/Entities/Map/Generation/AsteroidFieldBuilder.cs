using System.Collections.Generic;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Grows asteroid fields around the already fixed lanes: rasterises the field rules, splits them
    /// into separate fields, drops specks, and gives each field volumes and decorative rocks.
    /// </summary>
    public sealed class AsteroidFieldBuilder
    {
        private readonly MapGenerationSettings _settings;
        private readonly RockScatter _rockScatter;

        public AsteroidFieldBuilder(MapGenerationSettings settings)
        {
            _settings = settings;
            _rockScatter = new RockScatter(settings);
        }

        public List<AsteroidField> Build(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapLane> lanes,
            Random random)
        {
            FieldGrid grid = new FieldGrid(size.Bounds, size.FieldCellSize);
            FieldDensity density = new FieldDensity(_settings, size, stations, nodes, lanes, random);
            for (int index = 0; index < grid.Count; index++)
            {
                grid.SetField(index, density.IsField(grid.GetCenter(index)));
            }

            float cellArea = grid.CellSize * grid.CellSize;
            List<List<int>> cellGroups = grid.FindFields();
            foreach (List<int> cells in cellGroups)
            {
                if (cells.Count * cellArea < size.MinFieldArea)
                {
                    foreach (int cell in cells)
                    {
                        grid.SetField(cell, false);
                    }
                }
            }

            int[] edgeDistances = grid.GetEdgeDistances();
            List<AsteroidField> fields = new List<AsteroidField>();
            foreach (List<int> cells in cellGroups)
            {
                if (cells.Count * cellArea < size.MinFieldArea)
                {
                    continue;
                }

                fields.Add(new AsteroidField(
                    FieldVolumeCover.Cover(grid, cells, edgeDistances),
                    _rockScatter.Scatter(grid, cells, size.RockDensity, random)));
            }

            return fields;
        }
    }
}
