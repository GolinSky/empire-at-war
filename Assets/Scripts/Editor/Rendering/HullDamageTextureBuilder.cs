using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Generates the tiling detail texture for ShipHullDamage.hlsl:
    /// R = height (dented plating), G = cracks, B = roughness, A = where cracks may glow.
    /// Deterministic: rebaking gives the same texture.
    /// </summary>
    public static class HullDamageTextureBuilder
    {
        private const int SIZE = 512;
        private const int CRACK_CELLS = 10;
        private const float CRACK_WIDTH = 0.08f;
        private const string TEXTURE_PATH = "Assets/Art/Textures/Vfx/HullDamageDetail.png";

        [MenuItem("Tools/Empire At War/Bake Hull Damage Texture")]
        public static void Build()
        {
            var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color32[SIZE * SIZE];
                Vector2[] crackPoints = CreateCrackPoints();
                for (int y = 0; y < SIZE; y++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        float u = x / (float)SIZE;
                        float v = y / (float)SIZE;
                        float height = Fbm(u, v, 8, 4, 11);
                        float crackMask = SmoothStep(0.35f, 0.6f, Fbm(u, v, 4, 2, 23));
                        float cracks = (1f - SmoothStep(0f, CRACK_WIDTH, CrackDistance(u, v, crackPoints))) *
                            crackMask;
                        float roughness = Mathf.Lerp(0.55f, 1f, Fbm(u, v, 32, 2, 37));
                        float ember = cracks * SmoothStep(0.45f, 0.7f, Fbm(u, v, 6, 2, 41));
                        pixels[y * SIZE + x] = new Color(height, cracks, roughness, ember);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(TEXTURE_PATH, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(TEXTURE_PATH, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TEXTURE_PATH);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        // GLSL smoothstep: 0 below edge0, 1 above edge1. Mathf.SmoothStep interpolates values instead.
        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        // Fractal value noise that tiles across the texture: every octave uses a whole number of cells.
        private static float Fbm(float u, float v, int cells, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int octave = 0; octave < octaves; octave++)
            {
                sum += amplitude * ValueNoise(u * cells, v * cells, cells, seed + octave * 101);
                total += amplitude;
                amplitude *= 0.5f;
                cells *= 2;
            }

            return sum / total;
        }

        private static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, period, seed);
            float b = Hash(x0 + 1, y0, period, seed);
            float c = Hash(x0, y0 + 1, period, seed);
            float d = Hash(x0 + 1, y0 + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Hash(int x, int y, int period, int seed)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        private static Vector2[] CreateCrackPoints()
        {
            var points = new Vector2[CRACK_CELLS * CRACK_CELLS];
            for (int y = 0; y < CRACK_CELLS; y++)
            {
                for (int x = 0; x < CRACK_CELLS; x++)
                {
                    points[y * CRACK_CELLS + x] = new Vector2(x + Hash(x, y, CRACK_CELLS, 57),
                        y + Hash(x, y, CRACK_CELLS, 59)) / CRACK_CELLS;
                }
            }

            return points;
        }

        // Distance to the nearest Voronoi edge (F2 - F1), wrapped so the cracks tile.
        private static float CrackDistance(float u, float v, Vector2[] points)
        {
            int cellX = Mathf.FloorToInt(u * CRACK_CELLS);
            int cellY = Mathf.FloorToInt(v * CRACK_CELLS);
            float nearest = float.MaxValue;
            float second = float.MaxValue;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = cellX + dx;
                    int y = cellY + dy;
                    int wrappedX = ((x % CRACK_CELLS) + CRACK_CELLS) % CRACK_CELLS;
                    int wrappedY = ((y % CRACK_CELLS) + CRACK_CELLS) % CRACK_CELLS;
                    Vector2 offset = new Vector2(x - wrappedX, y - wrappedY) / CRACK_CELLS;
                    float distance = Vector2.Distance(new Vector2(u, v), points[wrappedY * CRACK_CELLS + wrappedX] + offset);
                    if (distance < nearest)
                    {
                        second = nearest;
                        nearest = distance;
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }

            return (second - nearest) * CRACK_CELLS;
        }
    }
}
