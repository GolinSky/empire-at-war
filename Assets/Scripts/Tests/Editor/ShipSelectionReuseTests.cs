using EmpireAtWar.Entities.ShipUi;
using System;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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
                group.Configure(default(ShipType), null, Entries(oldHealth, 4), model, _ => clicks++, _ => { });
                group.gameObject.SetActive(true);
                ShipUi[] initial = group.GetComponentsInChildren<ShipUi>(true);
                Assert.That(initial.Length, Is.EqualTo(4));
                Assert.That(oldHealth.Subscribers, Is.EqualTo(4));

                group.Configure(default(ShipType), null, Entries(newHealth, 5), model, _ => clicks++, _ => { });
                Assert.That(oldHealth.Subscribers, Is.Zero);
                Assert.That(newHealth.Subscribers, Is.EqualTo(5));

                group.Configure(default(ShipType), null, Entries(oldHealth, 2), model, _ => clicks++, _ => { });
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

        private static ShipUiEntry[] Entries(HealthStub health, int count) => Enumerable.Range(0, count)
            .Select(_ => new ShipUiEntry(Array.Empty<ShipAbilitySlot>(), health, null, () => { })).ToArray();

        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { Subscribers++; } remove { Subscribers--; } }
            public int Subscribers { get; private set; }
            public ShipClass ShipClass => ShipClass.Capital;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
