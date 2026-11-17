using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.ControlGroups;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ShipUi;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class ControlGroupPreviewTests
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Ui/ControlGroups/ControlGroupUi.prefab";

        [Test]
        public void Icons_UseHeroThenStrongestDistinctTypeRegardlessOfCountsAndMemberOrder()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>("Assets/Settings/Data/Factions/Shared/FactionCatalog.asset");
            var icons = AssetDatabase.LoadAssetAtPath<ShipUiData>("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
            var selector = new ControlGroupIconSelector(catalog, icons);
            var hero = new PreviewEntity(UnitTypeId.Squadron(SquadronType.DarthVaderTIEAdvanced), ShipClass.HeroStrikecraft);
            var destroyer = new PreviewEntity(UnitTypeId.Ship(ShipType.Executor), ShipClass.Dreadnought);
            var fighter = new PreviewEntity(UnitTypeId.Squadron(SquadronType.TIEFighter), ShipClass.Fighter);
            var members = new List<IEntity> { fighter, fighter, fighter, destroyer, destroyer, hero };
            selector.Select(members, out Sprite first, out Sprite second);
            Assert.That(first, Is.SameAs(icons.GetSquadronIcon(SquadronType.DarthVaderTIEAdvanced)));
            Assert.That(second, Is.SameAs(icons.GetShipIcon(ShipType.Executor)));
            members.Reverse();
            selector.Select(members, out Sprite reversedFirst, out Sprite reversedSecond);
            Assert.That(reversedFirst, Is.SameAs(first));
            Assert.That(reversedSecond, Is.SameAs(second));
            selector.Select(new IEntity[] { fighter, fighter }, out first, out second);
            Assert.That(first, Is.SameAs(icons.GetSquadronIcon(SquadronType.TIEFighter)));
            Assert.That(second, Is.Null, "A single type must not fill both preview icons.");
        }

        [Test]
        public void HeroSquadron_PrecedesDreadnought()
        {
            Assert.That(ControlGroupIconPriority.Compare(
                UnitTypeId.Squadron(SquadronType.DarthVaderTIEAdvanced), ShipClass.HeroStrikecraft, true,
                UnitTypeId.Ship(ShipType.Executor), ShipClass.Dreadnought, false), Is.LessThan(0));
        }

        [TestCase(ShipClass.Dreadnought, ShipClass.BattleCruiser)]
        [TestCase(ShipClass.BattleCruiser, ShipClass.Capital)]
        [TestCase(ShipClass.Capital, ShipClass.HeavyCruiser)]
        [TestCase(ShipClass.HeavyCruiser, ShipClass.Cruiser)]
        [TestCase(ShipClass.Cruiser, ShipClass.Frigate)]
        [TestCase(ShipClass.Frigate, ShipClass.Corvette)]
        [TestCase(ShipClass.Corvette, ShipClass.Bomber)]
        [TestCase(ShipClass.Bomber, ShipClass.Interceptor)]
        [TestCase(ShipClass.Interceptor, ShipClass.Fighter)]
        public void ClassStrength_PrecedesTypeOrdinal(ShipClass stronger, ShipClass weaker)
        {
            Assert.That(ControlGroupIconPriority.Compare(
                UnitTypeId.Ship(ShipType.ISDII), stronger, false,
                UnitTypeId.Ship(ShipType.Venator), weaker, false), Is.LessThan(0));
        }

        [Test]
        public void SameClass_UsesStableTypeOrdering()
        {
            Assert.That(ControlGroupIconPriority.Compare(
                UnitTypeId.Squadron(SquadronType.XWing), ShipClass.Fighter, false,
                UnitTypeId.Squadron(SquadronType.Vulture), ShipClass.Fighter, false), Is.GreaterThan(0));
        }

        [Test]
        public void Prefab_HasTenOrderedSlotsAndExplicitReferences()
        {
            ControlGroupUi prefab = AssetDatabase.LoadAssetAtPath<ControlGroupUi>(PREFAB_PATH);
            Assert.That(prefab, Is.Not.Null);
            RectTransform root = (RectTransform)prefab.transform;
            Assert.That(root.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(root.anchorMax, Is.EqualTo(root.anchorMin));
            Assert.That(root.pivot, Is.EqualTo(root.anchorMin));
            var serialized = new SerializedObject(prefab);
            Assert.That(serialized.FindProperty("canvasGroup").objectReferenceValue, Is.Not.Null);
            SerializedProperty slots = serialized.FindProperty("slots");
            Assert.That(slots.arraySize, Is.EqualTo(10));
            for (int index = 0; index < slots.arraySize; index++)
            {
                var slot = (ControlGroupSlotView)slots.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.That(slot.name, Is.EqualTo("Slot" + ((index + 1) % 10)));
                var slotRect = (RectTransform)slot.transform;
                var background = (RectTransform)slotRect.GetChild(0);
                Assert.That(background.name, Is.EqualTo("Background"));
                Assert.That(background.sizeDelta, Is.EqualTo(slotRect.sizeDelta));
                Assert.That(background.GetComponent<Image>().color.a, Is.EqualTo(1f));
                var references = new SerializedObject(slot);
                foreach (string name in new[] { "countText", "emptyMark", "firstIcon", "secondIcon", "selectionMark", "emptyOutline" })
                    Assert.That(references.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
                SerializedProperty outline = references.FindProperty("outline");
                Assert.That(outline.arraySize, Is.EqualTo(4));
                for (int edge = 0; edge < outline.arraySize; edge++)
                    Assert.That(outline.GetArrayElementAtIndex(edge).objectReferenceValue, Is.Not.Null);
            }
            foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            Assert.That(prefab.GetComponent<Graphic>(), Is.Null, "Transparent root must not cover gameplay.");
        }

        [Test]
        public void SlotZero_RendersLastSlotAndEmptyStateRemovesOldIcons()
        {
            ControlGroupUi prefab = AssetDatabase.LoadAssetAtPath<ControlGroupUi>(PREFAB_PATH);
            ControlGroupUi view = Object.Instantiate(prefab);
            var texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            try
            {
                view.RenderSlot(0, 7, sprite, null, false);
                var viewProperties = new SerializedObject(view);
                var last = new SerializedObject(viewProperties.FindProperty("slots").GetArrayElementAtIndex(9).objectReferenceValue);
                var first = new SerializedObject(viewProperties.FindProperty("slots").GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.That(((TMP_Text)last.FindProperty("countText").objectReferenceValue).text, Is.EqualTo("7"));
                Assert.That(((TMP_Text)first.FindProperty("countText").objectReferenceValue).text, Is.Empty);
                Assert.That(((Image)last.FindProperty("firstIcon").objectReferenceValue).enabled, Is.True);
                Assert.That(((GameObject)last.FindProperty("emptyOutline").objectReferenceValue).activeSelf, Is.False);
                view.RenderSlot(0, 0, null, null, false);
                Assert.That(((TMP_Text)last.FindProperty("countText").objectReferenceValue).text, Is.Empty);
                Assert.That(((GameObject)last.FindProperty("emptyMark").objectReferenceValue).activeSelf, Is.True);
                Assert.That(((Image)last.FindProperty("firstIcon").objectReferenceValue).enabled, Is.False);
                Assert.That(((GameObject)last.FindProperty("emptyOutline").objectReferenceValue).activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private sealed class PreviewEntity : IEntity
        {
            private readonly IUnitTypeFacade _type;
            public PreviewEntity(UnitTypeId type, ShipClass shipClass)
            {
                _type = new UnitTypeFacade(type);
                HealthModel = new PreviewHealth(shipClass);
            }
            public long Id => 1;
            public PlayerId Owner => new PlayerId(0);
            public IHealthModelObserver HealthModel { get; }
            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                facade = _type is TFacade match ? match : default;
                return _type is TFacade;
            }
            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                TryGetFacade(out TFacade facade) ? facade : throw new InvalidOperationException();
        }

        private sealed class PreviewHealth : IHealthModelObserver
        {
            public PreviewHealth(ShipClass shipClass) => ShipClass = shipClass;
            public ShipClass ShipClass { get; }
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1;
            public float HullPercentage => 1;
            public float Shields => 0;
            public float ShieldPercentage => 0;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => false;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => new PlayerId(0);
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
