using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.MiningFacility;
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

        [TestCase("Research", 1)]
        [TestCase("Level", 1)]
        [TestCase("Ship", 2)]
        [TestCase("Structure", 2)]
        [TestCase("Hero", 1)]
        public void PurchaseLimit_HidesCardUntilCapacityIsReleased(string kind, int maxCount)
        {
            var data = new FactionData();
            typeof(FactionData).GetField("<MaxCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, maxCount);
            typeof(FactionData).GetField("<BuildTime>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, 10);
            if (kind == "Hero")
                typeof(FactionData).GetField("isHero", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(data, true);
            UnitRequest request = kind switch
            {
                "Research" => new ResearchUnitRequest(data, ResearchType.ImprovedWeapons, 1),
                "Level" => new LevelUnitRequest(data, 1),
                "Structure" => new MiningFacilityUnitRequest(data, MiningFacilityType.CommonMiner),
                _ => new ShipUnitRequest(data, ShipType.HomeOne)
            };
            var model = new PlayerFactionModel(null, null) { SelectionType = SelectionType.Base };
            FactionUiController presenter = CreatePresenter(model);
            FactionUnitUi prefab = AssetDatabase.LoadAssetAtPath<FactionUnitUi>(
                "Assets/Prefabs/Ui/Factions/FactionUnit.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject root = new GameObject("Faction purchase limit test");
            FactionUi view = root.AddComponent<FactionUi>();
            FactionUnitUi card = Object.Instantiate(prefab, root.transform);

            try
            {
                card.SetData(data, view, request);
                ((List<FactionUnitUi>)typeof(FactionUi).GetField("_factionUnitsUi",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Add(card);
                view.SetModel(model);
                view.AssignPresenter(presenter);
                for (int count = 0; count < maxCount; count++)
                {
                    view.RefreshAvailability(0f, false);
                    Assert.That(card.gameObject.activeSelf, Is.True, "Cards below their own limit remain visible.");
                    model.QueueUnit(request);
                }

                Assert.That(presenter.IsUnitLimitReached(request), Is.True);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.False, "Reaching the purchase limit must hide the card.");

                Assert.That(model.TryCancelCurrentUnit(request.Id, out _), Is.True);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Cancelling a purchase must restore the card.");

                model.QueueUnit(request);
                model.Advance(10f * maxCount);
                view.RefreshAvailability(0f, false);
                bool hasLifetimeLimit = kind == "Structure" || kind == "Hero";
                Assert.That(card.gameObject.activeSelf, Is.EqualTo(!hasLifetimeLimit),
                    "Completed structures and heroes retain their reservation; ordinary queues release capacity.");

                if (kind == "Structure") model.ReleaseStructure(request);
                if (kind == "Hero") model.SetHeroReserved(ShipType.HomeOne, false);
                view.RefreshAvailability(0f, false);
                Assert.That(card.gameObject.activeSelf, Is.True, "Releasing a lifetime reservation restores the card.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FullProductionPipelines_DoNotReportAnUnqueuedUnitsOwnLimitReached()
        {
            var model = new PlayerFactionModel(null, null);
            var data = new FactionData();
            typeof(FactionData).GetField("<MaxCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(data, 1);
            for (int key = 1; key <= PlayerFactionModel.MAX_ACTIVE_PIPELINES; key++)
                model.QueueUnit(new LevelUnitRequest(data, key));
            var request = new ShipUnitRequest(data, ShipType.HomeOne);

            Assert.That(model.CanQueueUnit(request), Is.False);
            Assert.That(CreatePresenter(model).IsUnitLimitReached(request), Is.False,
                "Global pipeline capacity must not hide every remaining roster card.");
        }

        private static FactionUiController CreatePresenter(PlayerFactionModel model)
        {
            return new FactionUiController(
                uiService: null, placement: null, cancelRouter: null, factionService: null,
                model: model, research: null, unitRequestFactory: null, routeNavigation: null,
                tooltipService: null, economy: null, hud: null, battleState: null, roster: null,
                reinforcements: null, assets: null, shipsData: null, interfaceAudio: null);
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
