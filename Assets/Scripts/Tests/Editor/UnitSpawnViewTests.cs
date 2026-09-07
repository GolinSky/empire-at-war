using EmpireAtWar.Views.Reinforcement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitSpawnViewTests
    {
        private const string ACCLAMATOR_PREFAB_PATH =
            "Assets/Prefabs/Ui/Reinforcement/AcclamatorReinforcementView.prefab";
        private const string ACCLAMATOR_SHIP_PREFAB_PATH =
            "Assets/Prefabs/Models/Ships/AcclamatorShipView.prefab";

        [Test]
        public void SetRotation_AppliesProvidedWorldRotation()
        {
            GameObject gameObject = new GameObject(nameof(UnitSpawnViewTests));
            gameObject.SetActive(false);

            try
            {
                UnitSpawnView view = gameObject.AddComponent<UnitSpawnView>();
                Quaternion rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

                view.SetRotation(rotation);

                Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AcclamatorPrefab_UsesSameVisualAxesAsSpawnedShip()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ACCLAMATOR_PREFAB_PATH);
            GameObject shipRoot = PrefabUtility.LoadPrefabContents(ACCLAMATOR_SHIP_PREFAB_PATH);

            try
            {
                UnitSpawnView view = root.GetComponent<UnitSpawnView>();
                Quaternion stationFacing = Quaternion.LookRotation(Vector3.right, Vector3.up);

                view.SetRotation(stationFacing);

                Assert.That(
                    Quaternion.Angle(root.transform.rotation, stationFacing),
                    Is.LessThan(0.001f));
                Assert.That(root.transform.childCount, Is.GreaterThan(0));
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    Transform previewVisual = root.transform.GetChild(i);
                    Transform shipVisual = shipRoot.transform.Find($"Acclamator/{previewVisual.name}");
                    Assert.That(shipVisual, Is.Not.Null);
                    Assert.That(
                        Quaternion.Angle(previewVisual.localRotation, shipVisual.localRotation),
                        Is.LessThan(0.001f));
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                PrefabUtility.UnloadPrefabContents(shipRoot);
            }
        }
    }
}
