using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar
{
    public class NoiseTextureGenerator
    {
        public static int width = 256;
        public static int height = 256;

        [MenuItem("CUSTOM/Noise/Generate Main (Low-Freq) Noise")]
        public static void GenerateMainNoiseTexture()
        {
            float scale = 4f;
            Texture2D tex = GenerateBasicNoise(width, height, scale);

            string path = $"Assets/Graphics/Shaders/noise_main_{GetTimestamp()}.png";
            SaveTexture(tex, path);
        }

        [MenuItem("CUSTOM/Noise/Generate Detail (High-Freq) Noise")]
        public static void GenerateDetailNoiseTexture()
        {
            float scale = 10f;
            int octaves = 5;
            float persistence = 0.5f;
            Texture2D tex = GenerateFBMNoise(width, height, scale, octaves, persistence);

            string path = $"Assets/Graphics/Shaders/noise_detail_{GetTimestamp()}.png";
            SaveTexture(tex, path);
        }

        private static Texture2D GenerateBasicNoise(int width, int height, float scale)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = (float)x / width * scale;
                    float yCoord = (float)y / height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    tex.SetPixel(x, y, new Color(sample, sample, sample));
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateFBMNoise(int width, int height, float scale, int octaves, float persistence)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int o = 0; o < octaves; o++)
                    {
                        float xCoord = x / (float)width * scale * frequency;
                        float yCoord = y / (float)height * scale * frequency;
                        float sample = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;
                        noiseHeight += sample * amplitude;

                        amplitude *= persistence;
                        frequency *= 2f;
                    }

                    float normalized = Mathf.InverseLerp(-1f, 1f, noiseHeight);
                    tex.SetPixel(x, y, new Color(normalized, normalized, normalized));
                }
            }

            tex.Apply();
            return tex;
        }

        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); // Ensure directory exists
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log($"Saved: {path}");
        }
    }
}
