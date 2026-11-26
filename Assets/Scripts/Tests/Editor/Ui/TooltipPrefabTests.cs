using System.Linq;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class TooltipPrefabTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab";
        private const string MATCHUP_ROW_PATH = "Assets/Prefabs/Ui/Tooltip/TooltipMatchupRow.prefab";
        private const string ICON_DATA_PATH = "Assets/Settings/Data/Tooltip/TooltipIconData.asset";
        private const string MATCHUP_RULES_PATH = "Assets/Settings/Data/Units/MatchupRules.asset";

        [TestCase("MainMenu/SkirmishUi", 14)]
        [TestCase("MainMenu/SurvivalUi", 7)]
        [TestCase("MainMenu/SaveBrowserUi", 1)]
        [TestCase("Popups/SteamInvitePopupUi", 1)]
        public void MenuTooltipTriggersAreRegistered(string path, int expectedCount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Ui/{path}.prefab");
            TooltipHoverView view = prefab.GetComponentInChildren<TooltipHoverView>(true);
            Assert.That(view, Is.Not.Null);
            SerializedProperty triggers = new SerializedObject(view).FindProperty("triggers");
            Assert.That(triggers.arraySize, Is.EqualTo(expectedCount));
            var registered = Enumerable.Range(0, triggers.arraySize)
                .Select(index => triggers.GetArrayElementAtIndex(index).objectReferenceValue)
                .ToArray();
            Assert.That(registered, Is.EquivalentTo(prefab.GetComponentsInChildren<TooltipTrigger>(true)));
        }

        [Test]
        public void PrefabHasConcreteTooltipViewAndCanvasGroup()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<BaseUi>(), Is.InstanceOf<TooltipUi>());
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
        }

        [Test]
        public void PanelAndReusableRowsHaveNoRaycastTargets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Ui/Tooltip" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False, prefab.name + "/" + graphic.name);
            }
        }

        [Test]
        public void MatchupIconsFitRowSlotsAndResolve()
        {
            var icons = AssetDatabase.LoadAssetAtPath<TooltipIconData>(ICON_DATA_PATH);
            int slots = new SerializedObject(AssetDatabase.LoadAssetAtPath<TooltipMatchupRowView>(MATCHUP_ROW_PATH))
                .FindProperty("icons").arraySize;
            var rules = AssetDatabase.LoadAssetAtPath<MatchupRules>(MATCHUP_RULES_PATH);
            foreach (MatchupGroup group in rules.Groups)
            foreach (FactionType viewer in System.Enum.GetValues(typeof(FactionType)))
            {
                int shown = group.Icons.Count(icon => icon.Faction != viewer) +
                            (string.IsNullOrEmpty(group.NeutralIconKey) ? 0 : 1);
                Assert.That(shown, Is.InRange(1, slots), group.Label + " seen by " + viewer);
            }
            foreach (MatchupGroup group in rules.Groups)
            {
                foreach (MatchupIcon icon in group.Icons)
                    Assert.That(icons.Resolve(icon.IconKey), Is.Not.Null, group.Label);
                if (!string.IsNullOrEmpty(group.NeutralIconKey))
                    Assert.That(icons.Resolve(group.NeutralIconKey), Is.Not.Null, group.Label);
            }
        }

        [Test]
        public void FactionUnitIconKeysResolve()
        {
            var icons = AssetDatabase.LoadAssetAtPath<TooltipIconData>(ICON_DATA_PATH);
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FactionDefinition)))
            {
                var faction = AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (FactionData entry in faction.Ships.Values.Concat(faction.Squadrons.Values))
                {
                    Assert.That(entry.IconKey, Is.Not.Empty, $"{faction.name}/{entry.Name}");
                    Assert.That(icons.Resolve(entry.IconKey), Is.Not.Null, $"{faction.name}/{entry.Name}");
                }
            }
        }

        [Test]
        public void AllTooltipWidgetsHaveAssignedSerializedReferences()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Ui/Tooltip" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component.GetType().Namespace != typeof(TooltipUi).Namespace) continue;
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Assert.That(property.objectReferenceValue, Is.Not.Null,
                                component.GetType().Name + "." + property.propertyPath);
                }
            }
        }
    }
}
