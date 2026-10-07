using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Environment;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class MatchNebulaTests
    {
        private const string PREFAB = "Assets/Prefabs/Vfx/SkirmishVolumetricNebulaClouds.prefab";

        [Test]
        public void MatchLoads_SelectAllCompositionsWithoutRepeatingOrChangingGameplayRandom()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            var awake = typeof(MatchNebula).GetMethod("Awake",BindingFlags.Instance | BindingFlags.NonPublic);
            var seen = new HashSet<int>();
            int previous = -1;
            var randomState = Random.state;
            for (int i = 0; i < 60; i++)
            {
                var instance = Object.Instantiate(source);
                try
                {
                    var nebula = instance.GetComponent<MatchNebula>();
                    awake.Invoke(nebula,null);
                    Assert.That(nebula.SelectedVariant,Is.InRange(0,2));
                    Assert.That(nebula.SelectedVariant,Is.Not.EqualTo(previous));
                    previous = nebula.SelectedVariant;
                    seen.Add(previous);
                    Assert.That(instance.GetComponentInChildren<MeshRenderer>().sharedMaterial,Is.Not.Null);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            Assert.That(seen.Count,Is.EqualTo(3));
            Assert.That(Random.state,Is.EqualTo(randomState));
        }

        [Test]
        public void Prefab_HasOneVolumeAndThreeDistinctBakedFieldsWithValidShaders()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            Assert.That(source.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(1));
            Assert.That(source.GetComponentsInChildren<ParticleSystem>(),Is.Empty);
            var serialized = new SerializedObject(source.GetComponent<MatchNebula>());
            Assert.That(serialized.FindProperty("volumeRenderer").objectReferenceValue,Is.Not.Null);
            var variants = serialized.FindProperty("variants");
            Assert.That(variants.arraySize,Is.EqualTo(3));
            var textures = new HashSet<Texture>();
            for (int i = 0; i < variants.arraySize; i++)
            {
                var material = (Material)variants.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(material.shader.isSupported,Is.True);
                Assert.That(ShaderUtil.ShaderHasError(material.shader),Is.False);
                var texture = material.GetTexture("_CloudField") as Texture3D;
                Assert.That(texture,Is.Not.Null);
                Assert.That(texture.depth,Is.GreaterThan(1));
                Assert.That(material.GetTexture("_DetailTex"),Is.Not.Null);
                Assert.That(textures.Add(texture),Is.True);
            }
        }

        [Test]
        public void Stars_AreStationaryAndPersistForTheMatch()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vfx/SkirmishVfx.prefab");
            var stars = source.GetComponentsInChildren<ParticleSystem>();
            Assert.That(stars.Length,Is.EqualTo(2));
            foreach (var system in stars)
            {
                Assert.That(system.main.startSpeed.constant,Is.Zero);
                Assert.That(system.main.startLifetime.constant,Is.GreaterThan(86400));
                Assert.That(system.emission.rateOverTime.constant,Is.Zero);
                Assert.That(system.emission.burstCount,Is.EqualTo(1));
                Assert.That(system.colorOverLifetime.enabled,Is.False);
                Assert.That(system.sizeOverLifetime.enabled,Is.False);
                Assert.That(ShaderUtil.ShaderHasError(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader),Is.False);
            }
        }
    }
}
