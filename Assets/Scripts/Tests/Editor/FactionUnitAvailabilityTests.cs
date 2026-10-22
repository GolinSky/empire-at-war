using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class FactionUnitAvailabilityTests
    {
        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void VisibilityRefresh_PreservesPurchaseRestrictions(
            bool affordable, bool battleEnded, bool atLimit)
        {
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject root = new GameObject("Faction availability test");
            FactionUi view = root.AddComponent<FactionUi>();
            FactionUnitUi card = Object.Instantiate(prefab, root.transform);

            try
            {
                FieldInfo cardsField = typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                ((List<FactionUnitUi>)cardsField.GetValue(view)).Add(card);
                typeof(FactionUi).GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, new UnlockedPresenter());
                typeof(FactionUi).GetField("_isRouteActive", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(view, true);
                MethodInfo refresh = typeof(FactionUi).GetMethod("RefreshUnitVisibility",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                SerializedObject serializedCard = new SerializedObject(card);
                Button button = (Button)serializedCard.FindProperty("purchaseButton").objectReferenceValue;

                card.RenderAvailability(true, affordable, false, battleEnded, atLimit);
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit),
                    "An unaffordable card must still deliver a click for rejected-purchase feedback.");

                // Selection, level and research updates all use this visibility refresh.
                refresh.Invoke(view, new object[] { SelectionType.None });
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.True);
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit), "Reselection must preserve purchase restrictions.");

                card.RenderAvailability(true, affordable, false, battleEnded, atLimit);
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(button.interactable, Is.EqualTo(!battleEnded && !atLimit), "Repeated refreshes must preserve cached availability.");

                card.RenderAvailability(true, true, false, false, false);
                Assert.That(button.interactable, Is.True, "Removing the restriction must enable purchases.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void UnavailableCards_HideOnSelectionAndRefreshUntilAvailable(bool unlocked, bool heroReserved)
        {
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject root = new GameObject("Faction visibility test");
            FactionUi view = root.AddComponent<FactionUi>();
            FactionUnitUi card = Object.Instantiate(prefab, root.transform);
            var model = new PlayerFactionModel(null, null) { SelectionType = SelectionType.Base };
            var presenter = new AvailabilityPresenter { Unlocked = unlocked, HeroReserved = heroReserved };
            var data = new FactionData();
            card.SetData(data, view, new ShipUnitRequest(data, ShipType.HomeOne));

            try
            {
                ((List<FactionUnitUi>)typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Add(card);
                view.SetModel(model);
                view.AssignPresenter(presenter);
                MethodInfo refresh = typeof(FactionUi).GetMethod("RefreshUnitVisibility",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.False, "Unavailable cards must be hidden on selection.");
                card.SetActive(true);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.False, "Live updates must hide locked units and reserved heroes.");

                presenter.Unlocked = true;
                presenter.HeroReserved = false;
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Unlocked units and released heroes must reappear.");

                presenter.Unlocked = unlocked;
                presenter.HeroReserved = heroReserved;
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.False, "New purchases and level changes must hide cards again.");
                refresh.Invoke(view, new object[] { SelectionType.None });
                refresh.Invoke(view, new object[] { SelectionType.Base });
                Assert.That(card.gameObject.activeSelf, Is.False, "Reselection must keep unavailable cards hidden.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class AvailabilityPresenter : IFactionPresenter
        {
            public bool Unlocked { get; set; }
            public bool HeroReserved { get; set; }
            public void TryPurchaseUnit(UnitRequest unitRequest) { }
            public bool IsUnitAvailable(FactionData data) => Unlocked;
            public bool IsUnitLimitReached(UnitRequest request) => HeroReserved;
            public int GetUnitSortOrder(UnitRequest request) => 0;
        }

        private sealed class UnlockedPresenter : IFactionPresenter
        {
            public void TryPurchaseUnit(UnitRequest unitRequest) { }
            public bool IsUnitAvailable(FactionData data) => true;
            public bool IsUnitLimitReached(UnitRequest request) => false;
            public int GetUnitSortOrder(UnitRequest request) => 0;
        }
    }
}
