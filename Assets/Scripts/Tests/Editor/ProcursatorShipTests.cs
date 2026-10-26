using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ProcursatorShipTests
    {
        private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/ProcursatorShipView.prefab";
        private const string DATA = "Assets/Settings/Data/Ship/Empire/ProcursatorShipData.asset";

        [Test]
        public void SavedShip_HasRequestedStatsAndNoInjectableHangar()
        {
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(data.Hull, Is.EqualTo(6500));
            Assert.That(data.Shields, Is.EqualTo(4000));
            Assert.That(data.Speed, Is.EqualTo(19));
            Assert.That(data.ShipClass, Is.EqualTo(ShipClass.Capital));
            Assert.That(data.Abilities, Is.EqualTo(new[] { ShipAbilityId.BoostWeaponPower }));
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            Assert.That(data.Range, Is.GreaterThanOrEqualTo(data.WeaponLoadout.Max(w => weapons.GetProfile(w.WeaponType).Range)));
            Assert.That(data.HangarBays, Is.Empty);
            Assert.That(view.GetComponentsInChildren<HangarComponent>(true), Is.Empty);
        }

        [Test]
        public void AllWeaponsAndSystems_AreUniqueTargetableAndHaveHealth()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            var health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            var weapons = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(health.ShipUnits.Count, Is.EqualTo(17));
            Assert.That(health.ShipUnits.Select(h => h.Id), Is.EquivalentTo(Enumerable.Range(0, 17)));
            Assert.That(weapons.Length, Is.EqualTo(15));
            Assert.That(weapons.All(health.ShipUnits.Contains), Is.True);
            foreach (var expected in new[] { (WeaponType.HeavyQuadTurbolaser, 3), (WeaponType.DualMediumTurboLaser, 2),
                (WeaponType.ProcursatorMediumTripleTurbolaser, 8), (WeaponType.MediumIonCannon, 2) })
                Assert.That(weapons.Count(w => w.WeaponType == expected.Item1), Is.EqualTo(expected.Item2));
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var model = new HealthModel(data, matrix, new CombatModifiers());
            var targets = health.ShipUnits.Select(h => new HardPointModel(h.HardPointType, h.Id)).ToArray();
            model.InitializeHardPoints(targets);
            Assert.That(targets.All(h => h.Health > 0), Is.True);
            var generator = targets.Single(h => h.HardPointType == HardPointType.ShieldGenerator);
            float multiplier = matrix.GetDamageMultiplier(DamageType.ProtonTorpedo, data.ShipClass);
            model.ApplyDamage(generator.Health / multiplier, DamageType.ProtonTorpedo, generator.Id);
            Assert.That(model.IsLostShieldGenerator, Is.True);
            Assert.That(model.Shields, Is.Zero);
            foreach (var target in targets)
            {
                target.ApplyDamage(target.Health);
                Assert.That(target.IsDestroyed, Is.True);
            }
            Assert.That(model.HasLiveHardPoints, Is.False);
        }

        [Test]
        public void FrontalAndBroadsideArcs_MatchActualHullSide()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            foreach (var weapon in view.GetComponentsInChildren<WeaponHardPoint>(true))
            {
                float facing = weapon.WeaponType == WeaponType.HeavyQuadTurbolaser || weapon.WeaponType == WeaponType.MediumIonCannon
                    ? 0 : Mathf.Sign(weapon.transform.localPosition.x) * 90;
                Assert.That((weapon.MinYaw + weapon.MaxYaw) * .5f, Is.EqualTo(facing).Within(.001f), weapon.name);
                Assert.That(weapon.MinYaw, Is.GreaterThanOrEqualTo(-180));
                Assert.That(weapon.MaxYaw, Is.LessThanOrEqualTo(180));
            }
        }

        [Test]
        public void VisualsAndCollision_AreVenatorRelativeAndFitBankEnvelope()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
            var collider = view.GetComponents<BoxCollider>().Single();
            Vector3 scale = view.transform.localScale;
            Assert.That(scale.x, Is.GreaterThan(0f).And.LessThan(float.PositiveInfinity));
            Assert.That(scale, Is.EqualTo(Vector3.one * scale.x));
            Vector3 worldSize = Vector3.Scale(collider.size, collider.transform.lossyScale);
            Assert.That(worldSize.z, Is.EqualTo(119.383362f * 1200 / 1137).Within(.001f));
            Assert.That(data.NavigationRadius, Is.GreaterThan(new Vector2(worldSize.x, worldSize.z).magnitude * .5f));
            var hull = view.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Procursator" && t.parent.name == "BankingBody");
            foreach (var renderer in hull.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
            foreach (var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var position = view.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                foreach (var angle in new[] { -data.BodyRotationMaxAngle, 0, data.BodyRotationMaxAngle })
                {
                    float y = view.transform.TransformVector(Quaternion.Euler(0, 0, angle) * position).y;
                    Assert.That(y, Is.InRange(data.HullBottom - .001f, data.HullTop + .001f));
                }
            }
        }

        [Test]
        public void Injectables_RegisterOnlyCurrentHardpointsAndTeamColorView()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            var ship = view.GetComponent<EmpireAtWar.Ship.Ship>();
            var health = view.GetComponentsInChildren<HealthComponent>(true).Single();
            var teamColor = view.GetComponentsInChildren<EmpireAtWar.Components.TeamColor.UnitRenderStateComponent>(true).Single();
            Assert.That(ship.Bindings.Injectables, Is.All.Not.Null, "Deleted donor components must not remain in injection slots.");
            Assert.That(ship.Bindings.Injectables, Is.EquivalentTo(health.ShipUnits.Cast<MonoBehaviour>().Append(teamColor)));
        }

        [Test]
        public void RegisteredDataAndViews_ResolveProcursatorAssets()
        {
            var ships = AssetDatabase.LoadAssetAtPath<ShipsData>("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
            Assert.That(ships.GetShipDataPath(ShipType.Procursator), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA)));
            foreach (var path in new[] { VIEW, DATA })
                Assert.That(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address,
                    Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(path)));
        }
    }
}
