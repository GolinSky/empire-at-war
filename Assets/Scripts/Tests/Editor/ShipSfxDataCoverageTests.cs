using System;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipSfxDataCoverageTests
    {
        private const string DATA_PATH = "Assets/Settings/Data/Models/Audio/ShipSfxData.asset";

        [Test]
        public void EveryUnitResolvesAnEngineAndExplosion()
        {
            ShipSfxData data = AssetDatabase.LoadAssetAtPath<ShipSfxData>(DATA_PATH);
            Assert.That(data, Is.Not.Null);
            foreach (ShipType type in Enum.GetValues(typeof(ShipType)))
                AssertUnit(data.GetUnit(type), type.ToString());
            foreach (SquadronType type in Enum.GetValues(typeof(SquadronType)))
                AssertUnit(data.GetUnit(type), type.ToString());
        }

        [Test]
        public void EveryRandomClipPoolContainsOnlyAssignedClips()
        {
            ShipSfxData data = AssetDatabase.LoadAssetAtPath<ShipSfxData>(DATA_PATH);
            Assert.That(data, Is.Not.Null);
            using var serialized = new SerializedObject(data);
            SerializedProperty property = serialized.GetIterator();
            int checkedClips = 0;
            while (property.Next(true))
            {
                if (!property.isArray || property.propertyType == SerializedPropertyType.String ||
                    property.arrayElementType != "PPtr<$AudioClip>") continue;
                for (int i = 0; i < property.arraySize; i++)
                {
                    Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.InstanceOf<AudioClip>(),
                        $"{property.propertyPath}[{i}]");
                    checkedClips++;
                }
            }
            Assert.That(checkedClips, Is.GreaterThan(0));
        }

        [Test]
        public void AbilityCuesOutrankEveryAlarm()
        {
            ShipSfxData data = AssetDatabase.LoadAssetAtPath<ShipSfxData>(DATA_PATH);
            Assert.That(data, Is.Not.Null);
            float alarm = data.Alarm.Priority;
            foreach (ShipType type in Enum.GetValues(typeof(ShipType)))
                if (data.GetUnit(type).Alarm != null) alarm = Mathf.Max(alarm, data.GetUnit(type).Alarm.Priority);
            Assert.That(data.AbilityActivated.Priority, Is.GreaterThan(alarm));
            using var serialized = new SerializedObject(data);
            SerializedProperty abilities = serialized.FindProperty("abilities");
            for (int i = 0; i < abilities.arraySize; i++)
                Assert.That(abilities.GetArrayElementAtIndex(i).FindPropertyRelative("start.priority").floatValue,
                    Is.GreaterThan(alarm), $"abilities[{i}].start");
        }

        private static void AssertUnit(ShipUnitAudioProfile profile, string unit)
        {
            Assert.That(profile, Is.Not.Null, unit);
            Assert.That(profile.Engine, Is.Not.Null, unit);
            Assert.That(profile.Engine.Loop, Is.True, unit);
            Assert.That(profile.Engine.GetClip(), Is.Not.Null, unit);
            Assert.That(profile.Explosion, Is.Not.Null, unit);
            Assert.That(profile.Explosion.GetClip(), Is.Not.Null, unit);
        }
    }
}
