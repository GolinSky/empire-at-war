using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class DarthVaderSquadronTests
    {
        private const string DATA_FOLDER = "Assets/Settings/Data/Squadron/";
        private const string VIEW_FOLDER = "Assets/Prefabs/Models/Squadrons/";
        private const string UNIT = "DarthVaderTIEAdvanced";
        private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        [Test]
        public void SavedFormation_HasVaderLeadingSixTieEscorts()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_FOLDER + UNIT + "SquadronView.prefab");
            Assert.That(view, Is.Not.Null);
            FighterView[] fighters = view.GetComponentsInChildren<FighterView>(true).OrderBy(f => f.Id).ToArray();
            Assert.That(fighters.Select(f => f.Id), Is.EqualTo(Enumerable.Range(0, 7)));
            SerializedProperty flight = new SerializedObject(view.GetComponent<SquadronFlightComponent>()).FindProperty("fighters");
            Assert.That(flight.arraySize, Is.EqualTo(7));
            for (int i = 0; i < fighters.Length; i++)
            {
                Assert.That(flight.GetArrayElementAtIndex(i).objectReferenceValue, Is.EqualTo(fighters[i]));
                WeaponType expected = i == 0 ? WeaponType.DarthVaderLaser : WeaponType.DarthVaderEscortLaser;
                Assert.That(fighters[i].GetComponentsInChildren<WeaponHardPoint>(true).Select(g => g.WeaponType),
                    Is.EqualTo(new[] { expected, expected }), "Fighter " + i);
            }

            GameObject tieView = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW_FOLDER + "TIEFighterSquadronView.prefab");
            Mesh[] tieMeshes = tieView.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Distinct().ToArray();
            foreach (FighterView escort in fighters.Skip(1))
            {
                MeshFilter[] meshes = escort.Body.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(meshes, Is.Not.Empty);
                Assert.That(meshes.All(mesh => tieMeshes.Contains(mesh.sharedMesh)), Is.True, escort.name);
            }
            Assert.That(fighters[0].Body.GetComponentsInChildren<MeshFilter>(true)
                .Any(mesh => !tieMeshes.Contains(mesh.sharedMesh)), Is.True, "Vader must use his own TIE Advanced geometry.");
            MeshRenderer[] leaderRenderers = fighters[0].Body.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            Assert.That(leaderRenderers, Is.Not.Empty);
            Bounds leaderBounds = leaderRenderers[0].bounds;
            foreach (MeshRenderer renderer in leaderRenderers.Skip(1)) leaderBounds.Encapsulate(renderer.bounds);
            Assert.That(Mathf.Max(leaderBounds.size.x, leaderBounds.size.y, leaderBounds.size.z),
                Is.EqualTo(6f).Within(0.1f), "The imported model's scale must survive FBX root conversion.");
        }

        [Test]
        public void SavedWeapons_OutgunRepublicHeroesWithoutChangingOrdinaryTieLasers()
        {
            SquadronData vader = LoadData(UNIT);
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);
            Assert.That(vader.MemberCount, Is.EqualTo(7));
            Assert.That(vader.WeaponLoadout.Select(w => (w.WeaponType, w.Count)), Is.EquivalentTo(new[]
            {
                (WeaponType.DarthVaderLaser, 2), (WeaponType.DarthVaderEscortLaser, 12)
            }));
            float leaderDps = weapons.GetProfile(WeaponType.DarthVaderLaser).DamagePerSecond * 2;
            Assert.That(leaderDps, Is.EqualTo(400f).Within(0.01f));
            Assert.That(weapons.GetProfile(WeaponType.DarthVaderEscortLaser).DamagePerSecond,
                Is.EqualTo(weapons.GetProfile(WeaponType.TIEFighterLaser).DamagePerSecond).Within(0.001f));
            foreach (string hero in new[] { "PloKoonDelta7B", "ObiWanKenobiEta2", "MaceWinduEta2", "AnakinSkywalkerEta2" })
            {
                SquadronData republic = LoadData(hero);
                float dps = republic.WeaponLoadout.Sum(w => weapons.GetProfile(w.WeaponType).DamagePerSecond * w.Count);
                Assert.That(leaderDps, Is.GreaterThan(dps), hero);
                Assert.That(vader.CruiseSpeed, Is.LessThan(republic.CruiseSpeed), hero);
                Assert.That(vader.CombatSpeed, Is.LessThan(republic.CombatSpeed), hero);
            }
            Assert.That(vader.CruiseSpeed, Is.EqualTo(58f));
            Assert.That(vader.CombatSpeed, Is.EqualTo(66f));
        }

        [Test]
        public void SavedDurability_ProtectsHeroWithoutGivingEscortsHeroHealth()
        {
            SquadronData vader = LoadData(UNIT);
            SquadronData tie = LoadData("TIEFighter");
            Assert.That(vader.LeaderHull, Is.EqualTo(3200f));
            Assert.That(vader.LeaderShields, Is.EqualTo(2400f));
            Assert.That(vader.ProtectLeaderUntilEscortsLost, Is.True);
            Assert.That(vader.ShipClass, Is.EqualTo(ShipClass.HeroStrikecraft));
            Assert.That(vader.MemberHull, Is.EqualTo(tie.MemberHull));
            Assert.That(vader.MemberShields, Is.EqualTo(tie.MemberShields));
            Assert.That(vader.TotalHull, Is.EqualTo(3200f + 6 * tie.MemberHull));
            Assert.That(vader.TotalShields, Is.EqualTo(2400f + 6 * tie.MemberShields));
            SquadronData luke = LoadData("LukeRedSquadron");
            Assert.That(luke, Is.Not.Null);
            Assert.That(vader.TotalHull, Is.LessThan(luke.TotalHull));
            Assert.That(vader.TotalShields, Is.LessThan(luke.TotalShields));
            foreach (string guid in AssetDatabase.FindAssets("t:SquadronData", new[] { DATA_FOLDER }))
            {
                SquadronData other = AssetDatabase.LoadAssetAtPath<SquadronData>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == vader || other == luke) continue;
                Assert.That(vader.TotalHull, Is.GreaterThan(other.TotalHull), other.name);
                Assert.That(vader.TotalShields, Is.GreaterThan(other.TotalShields), other.name);
            }
        }

        [Test]
        public void SavedAbility_ReplenishesWingmenWithSixtySecondRecovery()
        {
            Assert.That(LoadData(UNIT).Abilities, Does.Contain(ShipAbilityId.ReplenishWingmen));
            ShipAbilityDefinition definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.ReplenishWingmen);
            Assert.That(definition.Settings, Is.TypeOf<ReplenishWingmenSettings>());
            Assert.That(definition.RecoveryDelay, Is.EqualTo(60f));
            Assert.That(definition.RequiresEnemyTarget, Is.False);
        }

        [Test]
        public void SavedRoster_RegistersOneEmpireHero()
        {
            FactionCatalog factions = AssetDatabase.LoadAssetAtPath<FactionCatalog>(
                "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset");
            Assert.That(factions.Get(FactionType.Empire).Squadrons.ContainsKey(SquadronType.DarthVaderTIEAdvanced), Is.True);
            var entry = factions.GetSquadronFactionData(SquadronType.DarthVaderTIEAdvanced);
            Assert.That(entry.IsHero, Is.True);
            Assert.That(entry.MaxCount, Is.EqualTo(1));
            Assert.That(entry.Icon, Is.Not.Null);
        }

        private static SquadronData LoadData(string unit) =>
            AssetDatabase.LoadAssetAtPath<SquadronData>(DATA_FOLDER + unit + "SquadronData.asset");
    }
}
