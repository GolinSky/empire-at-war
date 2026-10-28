using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Continues the field noise beyond the playable map without creating gameplay volumes.</summary>
    public sealed class DecorAsteroidBuilder
    {
        public const int MAX_INSTANCES = 15000;
        private const int MAX_CANDIDATES = 120000;
        private const float AREA_UNIT = 1000f;
        private const float UPRIGHT_TILT = 15f;
        private static readonly AsteroidSize[] SIZES = (AsteroidSize[])Enum.GetValues(typeof(AsteroidSize));
        private readonly MapGenerationSettings _settings;

        public DecorAsteroidBuilder(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public IReadOnlyList<AsteroidSpot> Build(MapSizeSettings size, FieldDensity density, Random random)
        {
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            float margin = size.EdgeMargin;
            float width = max.x - min.x + margin * 2f;
            float height = max.y - min.y + margin * 2f;
            int candidates = Mathf.Min(MAX_CANDIDATES,
                Mathf.CeilToInt(width * height / AREA_UNIT * size.RockDensity));
            float totalShare = 0f;
            foreach (AsteroidSize rockSize in SIZES)
                totalShare += _settings.GetRockLayer(rockSize).Share;

            List<AsteroidSpot> rocks = new List<AsteroidSpot>();
            int accepted = 0;
            for (int i = 0; i < candidates; i++)
            {
                Vector3 position = new Vector3(min.x - margin + Next(random) * width, 0f,
                    min.y - margin + Next(random) * height);
                float outside = Mathf.Max(Mathf.Max(min.x - position.x, position.x - max.x),
                    Mathf.Max(min.y - position.z, position.z - max.y));
                if (outside <= 0f || !density.IsNaturalField(position))
                    continue;

                float falloff = 1f - Mathf.SmoothStep(0f, 1f, outside / margin);
                if (Next(random) >= falloff)
                    continue;

                AsteroidSize rockSize = PickSize(totalShare, random);
                RockLayerSettings layer = _settings.GetRockLayer(rockSize);
                position.y = Next(random) < layer.DepthShare
                    ? Mathf.Lerp(_settings.FieldFloor, _settings.FieldCeiling, Next(random))
                    : (Next(random) * 2f - 1f) * layer.HeightJitter;
                Vector3 rotation = layer.IsTumbled
                    ? new Vector3(Next(random) * 360f, Next(random) * 360f, Next(random) * 360f)
                    : new Vector3((Next(random) * 2f - 1f) * UPRIGHT_TILT, Next(random) * 360f,
                        (Next(random) * 2f - 1f) * UPRIGHT_TILT);
                AsteroidSpot rock = new AsteroidSpot(position, rotation, rockSize,
                    Mathf.Lerp(layer.Scale.Min, layer.Scale.Max, Next(random)));
                accepted++;
                if (rocks.Count < MAX_INSTANCES)
                {
                    rocks.Add(rock);
                }
                else
                {
                    // Reservoir sampling keeps all four sides equally represented when the budget is reached.
                    int replacement = random.Next(accepted);
                    if (replacement < MAX_INSTANCES)
                        rocks[replacement] = rock;
                }
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
                    return size;
            }

            return SIZES[SIZES.Length - 1];
        }

        private static float Next(Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
