using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
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
    }
}
