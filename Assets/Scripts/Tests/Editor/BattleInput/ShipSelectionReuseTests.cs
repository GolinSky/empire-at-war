using EmpireAtWar.Entities.ShipUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class ShipSelectionReuseTests
    {
        [Test]
        public void Reconfigure_ReusesCardsAndReleasesOldHealthAndClickSubscriptions()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Ship/ShipGroupUi.prefab");
            ShipGroupUi root = prefab.GetComponent<ShipGroupUi>();
            ShipSelectionGroupUi template = (ShipSelectionGroupUi)new SerializedObject(root)
                .FindProperty("groupPrefab").objectReferenceValue;
            GameObject hoverObject = new GameObject("Test tooltip hover", typeof(TooltipHoverView));
            ShipSelectionGroupUi group = Object.Instantiate(template);
            HealthStub oldHealth = new HealthStub();
            HealthStub newHealth = new HealthStub();
            int clicks = 0;
            try
            {
                group.SetTooltipHover(hoverObject.GetComponent<TooltipHoverView>());
                ShipUiModel model = new ShipUiModel();
                group.Configure(default(ShipType), null, "Venator I", Entries(oldHealth, 4), model, _ => { }, _ => clicks++, _ => { }, default, () => { });
                group.gameObject.SetActive(true);
                ShipUi[] initial = group.GetComponentsInChildren<ShipUi>(true);
                Assert.That(initial.Length, Is.EqualTo(4));
                Assert.That(oldHealth.Subscribers, Is.EqualTo(4));

                group.Configure(default(ShipType), null, "Venator I", Entries(newHealth, 5), model, _ => { }, _ => clicks++, _ => { }, default, () => { });
                Assert.That(oldHealth.Subscribers, Is.Zero);
                Assert.That(newHealth.Subscribers, Is.EqualTo(5));

                group.Configure(default(ShipType), null, "Venator I", Entries(oldHealth, 2), model, _ => { }, _ => clicks++, _ => { }, default, () => { });
                Assert.That(newHealth.Subscribers, Is.Zero);
                Assert.That(oldHealth.Subscribers, Is.EqualTo(2));
                CollectionAssert.AreEquivalent(initial, group.GetComponentsInChildren<ShipUi>(true));
                ShipUi entry = group.GetComponentsInChildren<ShipUi>().First();
                Button select = (Button)typeof(ShipUi).GetField("disableSelectionButton",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry);
                select.onClick.Invoke();
                Assert.That(clicks, Is.EqualTo(1));

                group.Clear();
                Assert.That(oldHealth.Subscribers, Is.Zero);
                Assert.That(newHealth.Subscribers, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(group.gameObject);
                Object.DestroyImmediate(hoverObject);
            }
        }

        [Test]
        public void RightClick_UsesEachIndividualCardTargetAcrossCollapseAndReuse()
        {
            ShipGroupUi prefab = AssetDatabase.LoadAssetAtPath<ShipGroupUi>("Assets/Prefabs/Ui/Ship/ShipGroupUi.prefab");
            var template = (ShipSelectionGroupUi)new SerializedObject(prefab)
                .FindProperty("groupPrefab").objectReferenceValue;
            var hoverObject = new GameObject("Test tooltip hover", typeof(TooltipHoverView));
            ShipSelectionGroupUi group = Object.Instantiate(template);
            var requests = new List<(long Id, string Name, Vector2 Position)>();
            var pointer = new PointerEventData(null)
            {
                position = new Vector2(71f, 95f),
                button = PointerEventData.InputButton.Right
            };
            var model = new ShipUiModel();
            var health = new HealthStub();
            int selections = 0;
            try
            {
                group.SetTooltipHover(hoverObject.GetComponent<TooltipHoverView>());
                group.gameObject.SetActive(true);

                for (int count = 1; count <= 4; count++)
                {
                    requests.Clear();
                    Configure(count, 10 * count, "Venator I");
                    ShipCardContextInput[] inputs = group.GetComponentsInChildren<ShipCardContextInput>();
                    Assert.That(inputs, Has.Length.EqualTo(count));
                    foreach (ShipCardContextInput input in inputs)
                    {
                        Assert.That(input.GetComponent<Graphic>().raycastTarget, Is.True);
                        Click(input);
                    }
                    CollectionAssert.AreEquivalent(Enumerable.Range(10 * count, count)
                        .Select(id => ((long)id, "Venator I", pointer.position)), requests);
                }
                ShipCardContextInput[] original = group.GetComponentsInChildren<ShipCardContextInput>();
                Assert.That(selections, Is.Zero);

                requests.Clear();
                original[0].OnPointerDown(pointer);
                original[1].OnPointerDown(pointer);
                Configure(5, 50, "Venator I");
                Assert.That(group.GetComponentsInChildren<ShipCardContextInput>(), Is.Empty);
                var summary = (GameObject)new SerializedObject(group).FindProperty("summaryCard").objectReferenceValue;
                ExecuteEvents.ExecuteHierarchy(summary, pointer, ExecuteEvents.pointerClickHandler);
                original[1].OnPointerClick(pointer);
                Assert.That(requests, Is.Empty);

                Configure(2, 100, "Venator II");
                original[0].OnPointerClick(pointer);
                Assert.That(requests, Is.Empty, "The previous card's held press cannot target the rebound card.");
                foreach (ShipCardContextInput input in group.GetComponentsInChildren<ShipCardContextInput>())
                    Click(input);
                CollectionAssert.AreEquivalent(new[] { (100L, "Venator II", pointer.position),
                    (101L, "Venator II", pointer.position) }, requests);
                CollectionAssert.AreEquivalent(original, group.GetComponentsInChildren<ShipCardContextInput>(true));

                original[0].OnPointerDown(pointer);
                group.Clear();
                foreach (ShipCardContextInput input in original) input.OnPointerClick(pointer);
                Assert.That(requests, Has.Count.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(group.gameObject);
                Object.DestroyImmediate(hoverObject);
            }

            void Configure(int count, int firstId, string name) => group.Configure(default(ShipType), null, name,
                Entries(health, count, firstId), model, _ => { }, _ => selections++, _ => { }, default, () => { },
                (id, displayName, position) => requests.Add((id, displayName, position)));

            void Click(ShipCardContextInput input)
            {
                pointer.Reset();
                ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(pointer.used, Is.True);
            }
        }

        private static ShipUiEntry[] Entries(HealthStub health, int count, int firstId = 0) => Enumerable.Range(firstId, count)
            .Select(id => new ShipUiEntry(Array.Empty<ShipAbilitySlot>(), health,
                new Entity(health, null, Array.Empty<IEntityFacade>(), health.Owner, id,
                    Array.Empty<IPooledEntityEvents>(), Array.Empty<IHangarLaunchEvents>()))).ToArray();

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { Subscribers++; } remove { Subscribers--; } }
            public int Subscribers { get; private set; }
            public ShipClass ShipClass => ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
        }
    }
}
