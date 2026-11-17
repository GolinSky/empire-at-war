using System;
using System.Collections.Generic;
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
            SerializedProperty systems = config.FindProperty("systems");
            Assert.That(systems.arraySize, Is.GreaterThan(0));
            Assert.That(config.FindProperty("cullRadius").floatValue, Is.GreaterThan(0f));
            HashSet<Transform> domes = new HashSet<Transform>();
            for (int i = 0; i < systems.arraySize; i++)
            {
                ParticleSystem system = (ParticleSystem)systems.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(system, Is.Not.Null);
                Assert.That(system.main.playOnAwake, Is.False, "Domes stay dark until the well starts.");
                if (system.transform.parent != emitter.transform) domes.Add(system.transform.parent);
            }
            Assert.That(domes.Count, Is.EqualTo(DOME_COUNT), "Each hull dome has its own effects.");
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
