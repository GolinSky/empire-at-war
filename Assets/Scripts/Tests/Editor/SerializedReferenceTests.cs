using System;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.SceneContext.Skirmish;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Ui.Popups;
using EmpireAtWar.Views.Economy;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Views.Menu;
using EmpireAtWar.Views.MiniMap;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SerializedReferenceTests
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs";
        private const string LAYER_DATA_PATH = "Assets/Settings/Data/Models/Layer/LayerData.asset";
        private const string SHIP_MOVE_COMPONENT_FULL_NAME =
            "EmpireAtWar.Components.Ship.Movement.ShipMoveComponent";

        private static readonly SerializedReference[] REQUIRED_REFERENCES =
        {
            new SerializedReference(typeof(CameraService), "_camera"),
            new SerializedReference(typeof(MapObstacle), "_obstacleCollider"),
            new SerializedReference(typeof(SelectionComponent), "selectedCanvas"),
            new SerializedReference(typeof(SelectionComponent), "selectedImage"),
            new SerializedReference(typeof(BaseUi), "canvasGroup"),
            new SerializedReference(typeof(UiService), "defaultCanvas"),
            new SerializedReference(typeof(UiService), "dynamicCanvas"),
            new SerializedReference(typeof(UiService), "popupCanvas"),
            new SerializedReference(typeof(SkirmishPopupUi), "startingMoneySlider"),
            new SerializedReference(typeof(SkirmishPopupUi), "mapSizeDropdown"),
            new SerializedReference(typeof(MapLayoutView), "zonePrefab"),
            new SerializedReference(typeof(MapLayoutView), "miningSitePrefab"),
            new SerializedReference(typeof(MapLayoutView), "battleSitePrefab"),
            new SerializedReference(typeof(MapLayoutView), "fieldVolumePrefab"),
            new SerializedReference(typeof(MapLayoutView), "largeRockPrefab"),
            new SerializedReference(typeof(MapLayoutView), "mediumRockPrefab"),
            new SerializedReference(typeof(MapLayoutView), "zoneRoot"),
            new SerializedReference(typeof(MapLayoutView), "siteRoot"),
            new SerializedReference(typeof(MapLayoutView), "fieldRoot"),
            new SerializedReference(typeof(MapLayoutView), "borderLine"),
            new SerializedReference(typeof(MapLayoutView), "fogOfWarSystem"),
            new SerializedReference(typeof(MapInstaller), "settings"),
            new SerializedReference(typeof(MapInstaller), "mapLayoutView"),
            new SerializedReference(typeof(MapInstaller), "reinforcementZonesSystem"),
            new SerializedReference(typeof(MapInstaller), "captureSitesSystem"),
            new SerializedReference(typeof(SkirmishPopupUi), "closeButton"),
            new SerializedReference(typeof(SettingsPopupUi), "closeButton"),
            new SerializedReference(typeof(SettingsPopupUi), "qualitySettingsDropdown"),
            new SerializedReference(typeof(CoreGameUi), "reinforcementButton"),
            new SerializedReference(typeof(CoreGameUi), "miniMapRouteParent"),
            new SerializedReference(typeof(CoreGameUi), "contentRouteParent"),
            new SerializedReference(typeof(CoreGameUi), "buildPipelineRouteParent"),
            new SerializedReference(typeof(CoreGameUi), "panelImage"),
            new SerializedReference(typeof(CoreGameUi), "endGameUi"),
            new SerializedReference(typeof(EndGameUi), "outcomeText"),
            new SerializedReference(typeof(EndGameUi), "reasonText"),
            new SerializedReference(typeof(EndGameUi), "battlefieldText"),
            new SerializedReference(typeof(EndGameUi), "objectiveText"),
            new SerializedReference(typeof(EndGameUi), "playerFactionText"),
            new SerializedReference(typeof(EndGameUi), "enemyFactionText"),
            new SerializedReference(typeof(EndGameUi), "playerFleetText"),
            new SerializedReference(typeof(EndGameUi), "enemyFleetText"),
            new SerializedReference(typeof(EndGameUi), "playerBaseText"),
            new SerializedReference(typeof(EndGameUi), "enemyBaseText"),
            new SerializedReference(typeof(EndGameUi), "returnToMenuButton"),
            new SerializedReference(typeof(EconomyUi), "moneyText"),
            new SerializedReference(typeof(ReinforcementUi), "panelCanvasGroup"),
            new SerializedReference(typeof(MiniMapUi), "cameraFootprintView"),
            new SerializedReference(typeof(PauseMenuUi), "resumeButton"),
            new SerializedReference(typeof(PauseMenuUi), "exitButton"),
            new SerializedReference(typeof(PauseMenuUi), "menuPanel")
        };

        [Test]
        public void RequiredPrefabReferencesAreAssigned()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { PREFAB_FOLDER });
            int[] referenceCounts = new int[REQUIRED_REFERENCES.Length];
            int zoneViewCount = 0;
            int shipBuildViewCount = 0;
            int shipMoveComponentCount = 0;

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                MonoBehaviour[] components = prefab.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    SerializedObject serializedComponent = new SerializedObject(component);
                    string componentTypeName = component.GetType().FullName;
                    if (componentTypeName == SHIP_MOVE_COMPONENT_FULL_NAME)
                    {
                        AssertReferenceAssigned(
                            serializedComponent,
                            "lineRenderer",
                            $"{prefabPath}/{component.name}.lineRenderer");
                        AssertReferenceAssigned(
                            serializedComponent,
                            "bodyTransform",
                            $"{prefabPath}/{component.name}.bodyTransform");
                        shipMoveComponentCount++;
                    }

                    for (int i = 0; i < REQUIRED_REFERENCES.Length; i++)
                    {
                        SerializedReference reference = REQUIRED_REFERENCES[i];
                        if (!reference.ComponentType.IsInstanceOfType(component))
                        {
                            continue;
                        }

                        SerializedProperty property = serializedComponent.FindProperty(reference.FieldName);
                        string location = $"{prefabPath}/{component.name}.{reference.FieldName}";
                        Assert.That(property, Is.Not.Null, location);
                        Assert.That(property.objectReferenceValue, Is.Not.Null, location);
                        referenceCounts[i]++;
                    }

                    if (component is ReinforcementZoneView)
                    {
                        AssertNonCapturableZoneOwnerAssigned(serializedComponent, prefabPath);
                        zoneViewCount++;
                    }

                    if (component is ShipBuildUi)
                    {
                        AssertShipBuildPipelineAssigned(serializedComponent, prefabPath);
                        shipBuildViewCount++;
                    }
                }
            }

            for (int i = 0; i < referenceCounts.Length; i++)
            {
                Assert.That(
                    referenceCounts[i],
                    Is.GreaterThan(0),
                    $"No prefab contains {REQUIRED_REFERENCES[i].ComponentType.Name}.");
            }

            Assert.That(zoneViewCount, Is.GreaterThan(0));
            Assert.That(shipBuildViewCount, Is.GreaterThan(0));
            Assert.That(shipMoveComponentCount, Is.GreaterThan(0));
        }

        [Test]
        public void LayerConfigurationUsesValidProjectLayers()
        {
            LayerData layerData = AssetDatabase.LoadAssetAtPath<LayerData>(LAYER_DATA_PATH);
            Assert.That(layerData, Is.Not.Null, LAYER_DATA_PATH);

            AssertSingleLayer(layerData.UnitLayerMask, nameof(layerData.UnitLayerMask));
            AssertSingleLayer(layerData.ObstacleLayerMask, nameof(layerData.ObstacleLayerMask));
            AssertSingleLayer(layerData.DeadLayerMask, nameof(layerData.DeadLayerMask));
            Assert.That(LayerMask.NameToLayer("UI"), Is.GreaterThanOrEqualTo(0), "UI layer");
        }

        private static void AssertReferenceAssigned(
            SerializedObject serializedObject,
            string fieldName,
            string location)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.That(property, Is.Not.Null, location);
            Assert.That(property.objectReferenceValue, Is.Not.Null, location);
        }

        private static void AssertNonCapturableZoneOwnerAssigned(
            SerializedObject serializedView,
            string prefabPath)
        {
            SerializedProperty isCapturable = serializedView.FindProperty("_isCapturable");
            SerializedProperty startingOwner = serializedView.FindProperty("_startingOwner");
            Assert.That(isCapturable, Is.Not.Null, $"{prefabPath}._isCapturable");
            Assert.That(startingOwner, Is.Not.Null, $"{prefabPath}._startingOwner");

            if (isCapturable.boolValue)
            {
                return;
            }

            string ownerName = startingOwner.enumNames[startingOwner.enumValueIndex];
            Assert.That(ownerName, Is.EqualTo("Player").Or.EqualTo("Opponent"), $"{prefabPath}._startingOwner");
        }

        private static void AssertShipBuildPipelineAssigned(
            SerializedObject serializedUi,
            string prefabPath)
        {
            SerializedProperty pipelineView = serializedUi.FindProperty("pipelineView");
            Assert.That(pipelineView, Is.Not.Null, $"{prefabPath}.pipelineView");

            SerializedProperty canvasGroup = pipelineView.FindPropertyRelative("canvasGroup");
            Assert.That(canvasGroup, Is.Not.Null, $"{prefabPath}.pipelineView.canvasGroup");
            Assert.That(
                canvasGroup.objectReferenceValue,
                Is.Not.Null,
                $"{prefabPath}.pipelineView.canvasGroup");
        }

        private static void AssertSingleLayer(LayerMask mask, string fieldName)
        {
            int value = mask.value;
            Assert.That(value, Is.GreaterThan(0), fieldName);
            Assert.That(value & (value - 1), Is.Zero, fieldName);

            int layer = 0;
            while ((value >>= 1) != 0)
            {
                layer++;
            }

            Assert.That(LayerMask.LayerToName(layer), Is.Not.Empty, fieldName);
        }

        private readonly struct SerializedReference
        {
            public SerializedReference(
                Type componentType,
                string fieldName)
            {
                ComponentType = componentType;
                FieldName = fieldName;
            }

            public Type ComponentType { get; }
            public string FieldName { get; }
        }
    }
}
