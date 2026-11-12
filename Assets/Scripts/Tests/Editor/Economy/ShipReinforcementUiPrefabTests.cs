using EmpireAtWar;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using MPUIKIT;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EmpireAtWar.Entities.Reinforcement;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    public sealed class ShipReinforcementUiPrefabTests
    {
        private const string PREFAB_PATH =
            "Assets/Prefabs/Ui/Reinforcement/ShipReinforcementUi.prefab";

        [TestCase(PointerEventData.InputButton.Left, 1)]
        [TestCase(PointerEventData.InputButton.Right, 0)]
        [TestCase(PointerEventData.InputButton.Middle, 0)]
        public void Drag_OnlyLeftButtonStartsPlacement(PointerEventData.InputButton button, int expectedPlacements)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            try
            {
                SpawnShipUi view = root.GetComponent<SpawnShipUi>();
                var visitor = new ReinforcementVisitor();
                var request = new ShipUnitRequest(new FactionData(), ShipType.ISDII);
                ((ISpawnShipUi)view).Init(visitor, request, scrollObject.GetComponent<ScrollRect>());
                var pointer = new PointerEventData(eventSystemObject.GetComponent<EventSystem>())
                {
                    button = button,
                    pressPosition = Vector2.zero,
                    position = new Vector2(100f, 0f)
                };

                ((IInitializePotentialDragHandler)view).OnInitializePotentialDrag(pointer);
                ((IBeginDragHandler)view).OnBeginDrag(pointer);
                ((IDragHandler)view).OnDrag(pointer);
                ((IEndDragHandler)view).OnEndDrag(pointer);

                Assert.That(visitor.Placements, Is.EqualTo(expectedPlacements));
            }
            finally
            {
                Object.DestroyImmediate(scrollObject);
                Object.DestroyImmediate(eventSystemObject);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ShipReinforcementUiPrefab_IsConfiguredCorrectly()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            try
            {
                SpawnShipUi spawnShipUi = root.GetComponent<SpawnShipUi>();
                Assert.That(spawnShipUi, Is.Not.Null, "SpawnShipUi component must be on the root GameObject.");
                Assert.That(spawnShipUi, Is.InstanceOf<MonoBehaviour>(), "SpawnShipUi must inherit from MonoBehaviour.");

                SerializedObject serializedObj = new SerializedObject(spawnShipUi);
                SerializedProperty iconImageProp = serializedObj.FindProperty("iconImage");
                SerializedProperty bgImageProp = serializedObj.FindProperty("backgroundImage");
                SerializedProperty capacityTextProp = serializedObj.FindProperty("unitCapacityText");
                SerializedProperty countTextProp = serializedObj.FindProperty("unitCountText");

                Assert.That(iconImageProp.objectReferenceValue, Is.Not.Null, "iconImage must be bound.");
                Assert.That(bgImageProp.objectReferenceValue, Is.Not.Null, "backgroundImage must be bound.");
                Assert.That(capacityTextProp.objectReferenceValue, Is.Not.Null, "unitCapacityText must be bound.");
                Assert.That(countTextProp.objectReferenceValue, Is.Not.Null, "unitCountText must be bound.");

                TextMeshProUGUI capacityText = capacityTextProp.objectReferenceValue as TextMeshProUGUI;
                TextMeshProUGUI countText = countTextProp.objectReferenceValue as TextMeshProUGUI;

                Assert.That(capacityText, Is.Not.Null);
                Assert.That(countText, Is.Not.Null);

                // The core HUD uses fixed font sizes, so card text does not shrink per card.
                Assert.That(capacityText.enableAutoSizing, Is.False, "unitCapacityText must use a fixed font size.");
                Assert.That(countText.enableAutoSizing, Is.False, "unitCountText must use a fixed font size.");

                MPImage mpImage = root.GetComponent<MPImage>();
                Assert.That(mpImage, Is.Not.Null, "Root background must feature an MPImage component.");
                Assert.That(mpImage.DrawShape, Is.EqualTo(DrawShape.Rectangle), "Root MPImage must use Rectangle shape.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private sealed class ReinforcementVisitor : IReinforcementVisitor
        {
            public int Placements { get; private set; }
            public void Handle(ISpawnShipUi view) => Placements++;
            public void OnRelease(ISpawnShipUi view) { }
        }
    }
}
