using EmpireAtWar.Components.Ship.Selection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class HealthOverlayAnchorTests
    {
        private const string SHIP_PREFAB_PATH =
            "Assets/Prefabs/Models/Ships/MunificentShipView.prefab";
        private const string STATION_PREFAB_PATH =
            "Assets/Prefabs/Models/Stations/RepublicSpaceStationView.prefab";

        [TestCase(SHIP_PREFAB_PATH)]
        [TestCase(STATION_PREFAB_PATH)]
        public void SelectionWorldPosition_UsesExplicitSelectionCanvas(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SelectionComponent selection = root.GetComponent<SelectionComponent>();
                Transform selectedCanvas = root.transform.Find("SelectedCanvas");

                Assert.That(selection, Is.Not.Null);
                Assert.That(selectedCanvas, Is.Not.Null);
                Assert.That(selection.WorldPosition, Is.EqualTo(selectedCanvas.position));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void RepublicStation_SelectionAnchor_IsNotItsOffsetPrefabOrigin()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(STATION_PREFAB_PATH);
            try
            {
                SelectionComponent selection = root.GetComponent<SelectionComponent>();

                Assert.That(selection, Is.Not.Null);
                Assert.That(
                    Vector3.Distance(root.transform.position, selection.WorldPosition),
                    Is.GreaterThan(10f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
