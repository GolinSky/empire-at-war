using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Squadrons are selected and shown selected through their icon: fighters carry no selection ring,
    /// no hit colliders and no engine trails.
    /// </summary>
    [Category("Squadrons")]
    public sealed class SquadronPrefabVisualsTests
    {
        private static IEnumerable<string> SquadronPrefabPaths =>
            Directory.GetFiles("Assets/Prefabs/Models/Squadrons", "*SquadronView.prefab");

        [TestCaseSource(nameof(SquadronPrefabPaths))]
        public void SquadronView_HasNoSelectionRingCollidersOrTrails(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Assert.That(root.GetComponentsInChildren<SelectionComponent>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<TrailRenderer>(true), Is.Empty);
                Assert.That(root.GetComponent<EntityBindings>().Selection, Is.Empty);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCaseSource(nameof(SquadronPrefabPaths))]
        public void SquadronView_HasOneGunAndOneMountPerSecondaryWeapon(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                    "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
                foreach (FighterHardPoint fighter in root.GetComponentsInChildren<FighterHardPoint>(true))
                {
                    WeaponHardPoint[] mounts = fighter.GetComponentsInChildren<WeaponHardPoint>(true);
                    WeaponHardPoint[] guns = mounts.Where(mount => weapons.GetProfile(mount.WeaponType).Family is
                        WeaponFamily.FighterLaser or WeaponFamily.InterceptorLaser or WeaponFamily.HeroLaser).ToArray();
                    Assert.That(guns, Is.EqualTo(new[] { fighter.Gun }), fighter.name);
                    Assert.That(mounts.Select(mount => mount.WeaponType).Distinct().Count(),
                        Is.EqualTo(mounts.Length), fighter.name + " must not duplicate a warhead or ion channel.");
                    SerializedProperty secondary = new SerializedObject(fighter).FindProperty("additionalGuns");
                    Assert.That(Enumerable.Range(0, secondary.arraySize)
                        .Select(i => secondary.GetArrayElementAtIndex(i).objectReferenceValue),
                        Is.EquivalentTo(mounts.Where(mount => mount != fighter.Gun)), fighter.name);
                    foreach (FighterTorpedoHardPoint torpedo in mounts.OfType<FighterTorpedoHardPoint>())
                        Assert.That(new SerializedObject(torpedo).FindProperty("fighterIndex").intValue,
                            Is.EqualTo(fighter.Id), fighter.name + " must keep its own attack-run gate.");
                }

                WeaponHardPoint[] allMounts = root.GetComponentsInChildren<WeaponHardPoint>(true);
                var registered = root.GetComponentsInChildren<WeaponComponent>(true).SelectMany(component =>
                {
                    SerializedProperty entries = new SerializedObject(component).FindProperty("hardPoints");
                    return Enumerable.Range(0, entries.arraySize)
                        .Select(i => entries.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                });
                Assert.That(registered, Is.EquivalentTo(allMounts));
                Assert.That(root.GetComponent<EntityBindings>().Injectables, Has.None.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
