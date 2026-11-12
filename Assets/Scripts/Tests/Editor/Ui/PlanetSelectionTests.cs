using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class PlanetSelectionTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";

        [Test]
        public void EverySelectablePlanet_HasPreviewAndUsesTheSameBattleMap()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            using (var view = new SerializedObject(prefab.GetComponent<SkirmishUi>()))
            {
                var previews = new Dictionary<PlanetType, Object>
                {
                    [PlanetType.Coruscant] = view.FindProperty("coruscantPreview").objectReferenceValue,
                    [PlanetType.Kamino] = view.FindProperty("kaminoPreview").objectReferenceValue
                };
                SerializedProperty entries = view.FindProperty("planetPreviews.keyValue");
                for (int index = 0; index < entries.arraySize; index++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                    previews.Add((PlanetType)entry.FindPropertyRelative("key").intValue,
                        entry.FindPropertyRelative("value").objectReferenceValue);
                }

                var model = new SkirmishModel();
                var mapType = model.CreateBattle().Map.Type;
                foreach (PlanetType type in Enum.GetValues(typeof(PlanetType)))
                {
                    Assert.That(previews.ContainsKey(type), Is.True, $"{type} has no preview entry.");
                    Assert.That(previews[type], Is.Not.Null, $"{type} has no preview texture.");
                    model.SelectPlanet(type);
                    Assert.That(model.CreateBattle().Map.Planet, Is.EqualTo(type));
                    Assert.That(model.CreateBattle().Map.Type, Is.EqualTo(mapType));
                }
            }
        }
    }
}
