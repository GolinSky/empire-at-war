using System;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class InterdictorGravityWellTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/InterdictorStarDestroyerShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/InterdictorStarDestroyerShipData.asset";
        private const int DOME_COUNT = 4;

        [Test]
        public void InterdictView_BindsEmitterWithFourDomesAndParticleSystems()
        {
            ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            Assert.That(data.Abilities, Does.Contain(ShipAbilityId.Interdict));
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            GravityWellEmitter emitter = view.GetComponent<EntityBindings>().GravityWellEmitter;
            Assert.That(emitter, Is.Not.Null);
            SerializedObject config = new SerializedObject(emitter);
            SerializedProperty anchors = config.FindProperty("domeAnchors");
            SerializedProperty systems = config.FindProperty("systems");
            Assert.That(anchors.arraySize, Is.EqualTo(DOME_COUNT));
            Assert.That(systems.arraySize, Is.GreaterThan(0));
            for (int i = 0; i < anchors.arraySize; i++)
                Assert.That(anchors.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            for (int i = 0; i < systems.arraySize; i++)
                Assert.That(systems.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void MovementAnchor_IsCountedAndNeedsAMatchingAdd()
        {
            CombatModifiers modifiers = new CombatModifiers();
            int changes = 0;
            modifiers.Changed += () => changes++;

            modifiers.AddMovementAnchor();
            modifiers.AddMovementAnchor();
            Assert.That(modifiers.IsAnchored, Is.True);
            Assert.That(modifiers.HasTemporaryEffects, Is.True);
            modifiers.RemoveMovementAnchor();
            Assert.That(modifiers.IsAnchored, Is.True);
            modifiers.RemoveMovementAnchor();
            Assert.That(modifiers.IsAnchored, Is.False);
            Assert.That(modifiers.HasTemporaryEffects, Is.False);
            Assert.That(changes, Is.EqualTo(4));
            Assert.Throws<InvalidOperationException>(modifiers.RemoveMovementAnchor);
        }
    }
}
