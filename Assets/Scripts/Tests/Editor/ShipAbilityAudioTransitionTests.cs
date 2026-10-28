using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class ShipAbilityAudioTransitionTests
    {
        [Test]
        public void InitializingRestoredRecoveryDoesNotPlayACue()
        {
            var host = new GameObject("Restored ability audio test");
            var data = ScriptableObject.CreateInstance<ShipSfxData>();
            try
            {
                var profile = new ShipAbilityAudioProfile();
                Set(profile, "abilityId", ShipAbilityId.BoostEnginePower);
                Set(data, "abilities", new[] { profile });
                var slot = new ShipAbilitySlot(null, new ShipAbilityDefinition(), ShipAbilityId.BoostEnginePower);
                typeof(ShipAbilitySlot).GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(slot, new object[] { ShipAbilityState.Recovering, 10f });
                var service = new RecordingSfxService();
                var component = host.AddComponent<AudioShipComponent>();
                Set(component, "_data", data);
                Set(component, "_viewTransform", host.transform);
                Set(component, "_shipSfxService", service);
                component.InitializeAudio(new[] { slot });
                component.HandleAbilityChanged();
                Assert.That(service.Requests, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(data);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ActivationAndRecoveryRequestExactlyOneCue(bool hasSpecificClips)
        {
            var host = new GameObject("Ability audio test");
            var clip = AudioClip.Create("Cue", 1, 1, 44100, false);
            var data = ScriptableObject.CreateInstance<ShipSfxData>();
            try
            {
                var start = Profile(hasSpecificClips ? clip : null);
                var end = Profile(hasSpecificClips ? clip : null);
                var genericStart = Profile(clip);
                var genericEnd = Profile(clip);
                Set(data, "abilityActivated", genericStart);
                Set(data, "abilityCanceled", genericEnd);
                var sounds = new ShipAbilityAudioProfile();
                Set(sounds, "start", start);
                Set(sounds, "end", end);
                var slot = new ShipAbilitySlot(null, new ShipAbilityDefinition(), ShipAbilityId.BoostEnginePower);
                var service = new RecordingSfxService();
                var component = host.AddComponent<AudioShipComponent>();
                Set(component, "_shipSfxService", service);
                Set(component, "_data", data);
                Set(component, "_viewTransform", host.transform);
                Set(component, "_abilities", new[] { slot });
                Set(component, "_states", new[] { ShipAbilityState.Ready });
                Set(component, "_sounds", new[] { sounds });
                Set(component, "_initialized", true);

                typeof(ShipAbilitySlot).GetMethod("Activate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(slot, new object[] { null });
                component.HandleAbilityChanged();
                component.HandleAbilityChanged();
                Assert.That(service.Requests, Is.EqualTo(new[] { hasSpecificClips ? start : genericStart }));

                typeof(ShipAbilitySlot).GetMethod("Recover", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(slot, null);
                component.HandleAbilityChanged();
                component.HandleAbilityChanged();
                Assert.That(service.Requests, Is.EqualTo(new[]
                {
                    hasSpecificClips ? start : genericStart, hasSpecificClips ? end : genericEnd
                }));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(clip);
            }
        }

        private static SfxProfile Profile(AudioClip clip)
        {
            var profile = new SfxProfile();
            Set(profile, "clip", clip);
            return profile;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private sealed class RecordingSfxService : IShipSfxService
        {
            public readonly List<SfxProfile> Requests = new List<SfxProfile>();
            public bool TryPlayOneShot(ShipSfxHandle ship, SfxProfile sfx, Vector3 position)
            {
                Requests.Add(sfx);
                return true;
            }
            public bool TryPlayWeaponShot(ShipSfxHandle ship, WeaponProfile weapon, Transform muzzle) => false;
            public bool TryHoldLoop(ShipSfxHandle ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch) => false;
            public bool TryHoldEngine(ShipSfxHandle ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch) => false;
            public bool TryPlayVoice(AudioClip clip) => false;
            public void ReleaseShip(ShipSfxHandle ship) { }
        }
    }
}
