using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Fills a field with decorative rocks in large, medium and debris layers.</summary>
    public sealed class RockScatter
    {
        private const float AREA_UNIT = 1000f;
        private const float UPRIGHT_TILT = 15f;

        private static readonly AsteroidSize[] SIZES = (AsteroidSize[])Enum.GetValues(typeof(AsteroidSize));
        private readonly MapGenerationSettings _settings;

        public RockScatter(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public List<AsteroidSpot> Scatter(FieldGrid grid, IReadOnlyList<int> cells, float density, Random random)
        {
            float totalShare = 0f;
            foreach (AsteroidSize size in SIZES)
            {
                totalShare += _settings.GetRockLayer(size).Share;
            }

            float area = cells.Count * grid.CellSize * grid.CellSize;
            int count = Mathf.Max(1, Mathf.RoundToInt(area / AREA_UNIT * density));
            List<AsteroidSpot> rocks = new List<AsteroidSpot>(count);
            for (int i = 0; i < count; i++)
            {
                AsteroidSize size = PickSize(totalShare, random);
                RockLayerSettings layer = _settings.GetRockLayer(size);
                Vector3 position = grid.GetCenter(cells[random.Next(cells.Count)]) + new Vector3(
                    (Next(random) - 0.5f) * grid.CellSize,
                    0f,
                    (Next(random) - 0.5f) * grid.CellSize);
                // Depth rocks show the field's real height band; the rest keep to the battle plane.
                position.y = Next(random) < layer.DepthShare
                    ? Mathf.Lerp(_settings.FieldFloor, _settings.FieldCeiling, Next(random))
                    : (Next(random) * 2f - 1f) * layer.HeightJitter;
                Vector3 rotation = layer.IsTumbled
                    ? new Vector3(Next(random) * 360f, Next(random) * 360f, Next(random) * 360f)
                    : new Vector3((Next(random) * 2f - 1f) * UPRIGHT_TILT, Next(random) * 360f,
                        (Next(random) * 2f - 1f) * UPRIGHT_TILT);
                float scale = Mathf.Lerp(layer.Scale.Min, layer.Scale.Max, Next(random));
                rocks.Add(new AsteroidSpot(position: position, rotation: rotation, scale: scale, size: size));
            }

            return rocks;
        }

        private AsteroidSize PickSize(float totalShare, Random random)
        {
            float pick = Next(random) * totalShare;
            foreach (AsteroidSize size in SIZES)
            {
                pick -= _settings.GetRockLayer(size).Share;
                if (pick <= 0f)
                {
                    return size;
                }
            }

            return SIZES[SIZES.Length - 1];
        }

        private static float Next(Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
