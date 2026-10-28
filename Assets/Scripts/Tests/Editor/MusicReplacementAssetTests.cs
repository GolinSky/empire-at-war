using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.SceneService;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    [Category("MusicReplacement")]
    public sealed class MusicReplacementAssetTests
    {
        private const string MANIFEST = "Tools/Audio/MusicReplacementManifest.json";
        private const string CONFIGURATION = "Assets/Settings/Data/Models/Audio/MusicAudioData.asset";
        private const string MUSIC_ROOT = "Assets/Audio/Music";

        private static JObject ReadManifest() => JObject.Parse(File.ReadAllText(MANIFEST));

        [Test]
        public void Manifest_ContainsOnlyAcceptedBattleAndMenuTracks()
        {
            JToken[] tracks = ReadManifest()["tracks"].ToArray();
            Assert.That(tracks.Length, Is.EqualTo(53));
            Assert.That(tracks.Count(track => (string)track["role"] == "Battle"), Is.EqualTo(22));
            Assert.That(tracks.Count(track => (string)track["role"] == "Menu"), Is.EqualTo(31));
            Assert.That(tracks.Select(track => (string)track["source"]), Is.Unique);
            Assert.That(tracks.Select(track => (string)track["destination"]), Is.Unique);
            foreach (JToken track in tracks)
            {
                string path = (string)track["destination"];
                Assert.That(path, Does.StartWith(MUSIC_ROOT + "/" + (string)track["role"] + "/"));
            }

            string[] imported = AssetDatabase.FindAssets("t:AudioClip", new[]
                { MUSIC_ROOT + "/Battle", MUSIC_ROOT + "/Menu" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(imported, Is.EquivalentTo(tracks.Select(track => (string)track["destination"])));
        }

        [Test]
        public void SavedConfiguration_PreservesIdentityAndEveryFactionUsesCompleteBattleSet()
        {
            JObject manifest = ReadManifest();
            Assert.That(AssetDatabase.AssetPathToGUID(CONFIGURATION), Is.EqualTo((string)manifest["configurationGuid"]));
            var data = AssetDatabase.LoadAssetAtPath<MusicAudioData>(CONFIGURATION);
            Assert.That(data, Is.Not.Null);
            var serialized = new SerializedObject(data);
            string[] menu = PlaylistPaths(serialized.FindProperty("menuMusicData"));
            string[] expectedMenu = manifest["tracks"].Where(track => (string)track["role"] == "Menu")
                .Select(track => (string)track["destination"]).ToArray();
            string[] expectedBattle = manifest["tracks"].Where(track => (string)track["role"] == "Battle")
                .Select(track => (string)track["destination"]).ToArray();
            Assert.That(menu, Is.Unique.And.EquivalentTo(expectedMenu));
            Assert.That(data.GetMusicList(SceneType.MainMenu, default).Select(AssetDatabase.GetAssetPath), Is.EquivalentTo(expectedMenu));
            Assert.That(menu.Intersect(expectedBattle), Is.Empty);

            SerializedProperty factions = serialized.FindProperty("battleMusicData.keyValue");
            Assert.That(factions.arraySize, Is.EqualTo(4));
            int[] keys = new int[factions.arraySize];
            for (int index = 0; index < factions.arraySize; index++)
            {
                SerializedProperty entry = factions.GetArrayElementAtIndex(index);
                keys[index] = entry.FindPropertyRelative("key").intValue;
                string[] battle = PlaylistPaths(entry.FindPropertyRelative("value"));
                Assert.That(battle, Is.Unique.And.EquivalentTo(expectedBattle), $"Faction {keys[index]}");
                Assert.That(data.GetMusicList(SceneType.Battle, (FactionType)keys[index]).Select(AssetDatabase.GetAssetPath),
                    Is.EquivalentTo(expectedBattle), $"Runtime faction {keys[index]}");
            }
            Assert.That(keys, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void AcceptedAudio_ImportsWithStreamingSettingsAndPlayableMetadata()
        {
            foreach (JToken track in ReadManifest()["tracks"])
            {
                string path = (string)track["destination"];
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip, Is.Not.Null, path);
                Assert.That(clip.length, Is.GreaterThan(0f), path);
                Assert.That(clip.channels, Is.InRange(1, 2), path);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.forceToMono, Is.False, path);
                Assert.That(importer.ambisonic, Is.False, path);
                Assert.That(importer.loadInBackground, Is.True, path);
                AssertSettings(importer.defaultSampleSettings, path + " default");
                Assert.That(importer.ContainsSampleSettingsOverride("Standalone"), Is.True, path);
                AssertSettings(importer.GetOverrideSampleSettings("Standalone"), path + " Standalone");
            }
        }

        [Test]
        public void AcceptedAudio_IsByteIdenticalToRecordedSourceFiles()
        {
            foreach (JToken track in ReadManifest()["tracks"])
            {
                string path = (string)track["destination"];
                AssertFileIdentity(path, track);
            }
        }

        [Test]
        public void ShipAmbience_AndImportMetadataRemainUnchanged()
        {
            JToken[] retained = ReadManifest()["retained"].ToArray();
            Assert.That(retained.Length, Is.EqualTo(4));
            foreach (JToken file in retained) AssertFileIdentity((string)file["path"], file);
        }

        [Test]
        public void LegacyAudio_IsAbsentFromDiskAssetDatabaseAndSerializedReferences()
        {
            JToken[] legacy = ReadManifest()["legacy"].ToArray();
            Assert.That(legacy, Is.Not.Empty);
            var legacyGuids = legacy.Select(track => (string)track["guid"]).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (JToken track in legacy)
            {
                string path = (string)track["path"];
                Assert.That(File.Exists(path), Is.False, path);
                Assert.That(File.Exists(path + ".meta"), Is.False, path);
                // Unity retains GUID mappings for deleted assets until the Editor restarts.
                Assert.That(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets), Is.Empty, path);
            }

            var guidPattern = new Regex(@"guid:\s*([a-fA-F0-9]{32})");
            foreach (string path in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
            {
                bool isMeta = path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
                if (!isMeta && !new[] { ".asset", ".prefab", ".unity", ".mat", ".controller", ".overrideController", ".anim" }
                        .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) continue;
                using (var reader = new StreamReader(path))
                {
                    string firstLine = reader.ReadLine();
                    if (!isMeta && firstLine != "%YAML 1.1") continue;
                    foreach (Match match in guidPattern.Matches(firstLine + "\n" + reader.ReadToEnd()))
                        Assert.That(legacyGuids.Contains(match.Groups[1].Value), Is.False, path);
                }
            }
        }

        private static string[] PlaylistPaths(SerializedProperty playlist)
        {
            string[] paths = new string[playlist.arraySize];
            for (int index = 0; index < paths.Length; index++)
            {
                UnityEngine.Object clip = playlist.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.That(clip, Is.Not.Null, playlist.propertyPath);
                paths[index] = AssetDatabase.GetAssetPath(clip);
            }
            return paths;
        }

        private static void AssertFileIdentity(string path, JToken recorded)
        {
            Assert.That(new FileInfo(path).Length, Is.EqualTo((long)recorded["bytes"]), path);
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                string hash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                Assert.That(hash, Is.EqualTo(((string)recorded["sha256"]).ToLowerInvariant()), path);
            }
        }

        private static void AssertSettings(AudioImporterSampleSettings settings, string context)
        {
            Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), context);
            Assert.That(settings.quality, Is.EqualTo(0.7f).Within(0.0001f), context);
            Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.Streaming), context);
            Assert.That(settings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), context);
            Assert.That(settings.preloadAudioData, Is.False, context);
        }
    }
}
