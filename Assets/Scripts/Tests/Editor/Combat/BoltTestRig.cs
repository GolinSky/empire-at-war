using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    /// <summary>Shared setup for bolt flight tests: prefabs, stub targets, and reads into the bolt batch.</summary>
    internal static class BoltTestRig
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string PROJECTILES = "Assets/Prefabs/Vfx/Projectiles/";

        internal static BoltShot LoadBolt(string prefabName) =>
            AssetDatabase.LoadAssetAtPath<BoltShot>(PROJECTILES + prefabName + ".prefab");

        internal static DamageMatrixData LoadDamageMatrix() => AssetDatabase.LoadAssetAtPath<DamageMatrixData>(
            "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");

        internal static WeaponProfile SpeedProfile(float projectileSpeed)
        {
            var profile = new WeaponProfile();
            JsonUtility.FromJsonOverwrite("{\"projectileSpeed\":" + projectileSpeed + "}", profile);
            return profile;
        }

        internal static ImpactEffectPresenter Impacts(IImpactEffectView view) =>
            new ImpactEffectPresenter(view, LoadDamageMatrix());

        internal static float Launch(BoltFlightBatch flights, BoltShot prefab, WeaponProfile profile, Transform muzzle,
            Transform target, IHealthModelObserver health, ImpactEffectPresenter impacts, bool isHit = false,
            float lateSeconds = 0f) =>
            flights.Launch(prefab, profile, muzzle, target, target, Vector3.zero, health, impacts, 1f, 1f, isHit, null,
                lateSeconds);

        internal static BoltLayer Layer(BoltFlightBatch flights, BoltShot prefab) =>
            ((Dictionary<BoltShot, BoltLayer>)typeof(BoltFlightBatch).GetField("_layers", PRIVATE_INSTANCE)
                .GetValue(flights))[prefab];

        internal static ParticleSystem Particles(BoltLayer layer) =>
            (ParticleSystem)typeof(BoltLayer).GetField("_particles", PRIVATE_INSTANCE).GetValue(layer);

        internal static Vector3 Position(BoltFlight flight)
        {
            BoltPose pose = flight.Pose;
            return pose.Start + pose.Path * pose.Progress;
        }

        internal sealed class RecordingImpactView : IImpactEffectView
        {
            public readonly List<Vector3> Positions = new List<Vector3>();

            public void Emit(ImpactSurface surface, Vector3 position, Vector3 direction, float size) =>
                Positions.Add(position);
        }

        internal sealed class VisualHealth : IHealthModelObserver, IShieldTarget
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass { get; set; } = ShipClass.Capital;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 100f;
            public float HullPercentage => 1f;
            public float Shields => HasShields ? 100f : 0f;
            public float ShieldPercentage => HasShields ? 1f : 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields { get; set; }
            public PlayerId Owner => default;
            public IHardPointModel[] GetShipUnits(HardPointType type) => Array.Empty<IHardPointModel>();
            public Vector3 GetImpactPosition(Vector3 origin, Vector3 target, DamageType type) =>
                HasShields ? new Vector3(0f, 0f, Mathf.Max(origin.z, 40f)) : target;
            public bool ShowShieldImpact(Vector3 position, float damage) => false;
        }
    }
}
