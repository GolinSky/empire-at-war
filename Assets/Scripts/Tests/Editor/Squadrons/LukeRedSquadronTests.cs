using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Repository.Data;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class LukeRedSquadronTests
    {
        private const string UNIT = "LukeRedSquadron";
        private const string DATA_FOLDER = "Assets/Settings/Data/Squadron/";
        private const string VIEW = "Assets/Prefabs/Models/Squadrons/LukeRedSquadronSquadronView.prefab";
        private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/LukeRedSquadronReinforcementView.prefab";

        [Test]
        public void SavedFormation_BindsLukeAndFiveWingmenToHealthFlightAndWeapons()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(view, Is.Not.Null);
            FighterHardPoint[] fighters = view.GetComponentsInChildren<FighterHardPoint>(true).OrderBy(fighter => fighter.Id).ToArray();
            Assert.That(fighters, Is.Not.Empty);
            Assert.That(fighters.Select(fighter => fighter.Id), Is.EqualTo(Enumerable.Range(0, fighters.Length)));
            SerializedProperty flight = new SerializedObject(view.GetComponent<SquadronFlightComponent>()).FindProperty("fighters");
            SerializedProperty health = new SerializedObject(view.GetComponent<SquadronHealthComponent>()).FindProperty("fighters");
            Assert.That(flight.arraySize, Is.EqualTo(fighters.Length));
            Assert.That(health.arraySize, Is.EqualTo(fighters.Length));
            for (int i = 0; i < fighters.Length; i++)
            {
                Assert.That(flight.GetArrayElementAtIndex(i).objectReferenceValue, Is.EqualTo(fighters[i]));
                Assert.That(health.GetArrayElementAtIndex(i).objectReferenceValue, Is.EqualTo(fighters[i]));
                WeaponType expected = i == 0 ? WeaponType.LukeSkywalkerLaser : WeaponType.RedSquadronEscortLaser;
                Assert.That(fighters[i].GetComponentsInChildren<WeaponHardPoint>(true).Select(gun => gun.WeaponType),
                    Is.EqualTo(new[] { expected }), fighters[i].name);
            }

            WeaponHardPoint[] guns = view.GetComponentsInChildren<WeaponHardPoint>(true);
            Assert.That(guns.Select(gun => gun.Id).Distinct().Count(), Is.EqualTo(guns.Length));
            WeaponComponent weapons = view.GetComponentInChildren<WeaponComponent>(true);
            Assert.That(weapons, Is.Not.Null);
            SerializedProperty registered = new SerializedObject(weapons).FindProperty("hardPoints");
                        Assert.That(Enumerable.Range(0, registered.arraySize)
                .Select(index => registered.GetArrayElementAtIndex(index).objectReferenceValue), Is.EquivalentTo(guns));
        }

        [Test]
        public void SavedGeometry_UsesAotrXWingForEveryMember()
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            foreach (FighterHardPoint fighter in view.GetComponentsInChildren<FighterHardPoint>(true))
            {
                MeshRenderer[] renderers = fighter.Body.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(renderer => renderer.enabled).ToArray();
                Assert.That(renderers, Is.Not.Empty, fighter.name);
                Bounds bounds = renderers[0].bounds;
                foreach (MeshRenderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.z, Is.EqualTo(6f).Within(0.1f), fighter.name);
                foreach (MeshRenderer renderer in renderers)
                {
                    Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    Assert.That(AssetDatabase.GetAssetPath(mesh),
                        Is.EqualTo("Assets/Art/Models/RebellionShips/LukeRedSquadron/LukeRedSquadron.fbx"));
                    Assert.That(renderer.sharedMaterials, Is.Not.Empty);
                    Assert.That(renderer.sharedMaterials.All(material => material != null), Is.True);
                }
            }
        }

        [Test]
        public void SavedStats_ExceedEveryOtherSquadronWithoutGivingWingmenLeaderHealth()
        {
            SquadronData luke = LoadData();
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
            Assert.That(luke.MemberCount, Is.EqualTo(view.GetComponentsInChildren<FighterHardPoint>(true).Length));
            Assert.That(luke.LeaderHull, Is.GreaterThan(luke.MemberHull));
            Assert.That(luke.LeaderShields, Is.GreaterThan(luke.MemberShields));
            Assert.That(luke.MemberHull, Is.GreaterThan(0f));
            Assert.That(luke.TotalHull, Is.EqualTo(luke.LeaderHull + (luke.MemberCount - 1) * luke.MemberHull).Within(0.01f));
            Assert.That(luke.TotalShields, Is.EqualTo(luke.LeaderShields + (luke.MemberCount - 1) * luke.MemberShields).Within(0.01f));
            Assert.That(luke.ShipClass, Is.EqualTo(ShipClass.HeroStrikecraft));
            Assert.That(luke.CombatSpeed, Is.GreaterThanOrEqualTo(luke.CruiseSpeed));
            Assert.That(luke.CruiseSpeed, Is.GreaterThan(0f));
            Assert.That(luke.WeaponLoadout.Select(weapon => (weapon.WeaponType, weapon.Count)), Is.EquivalentTo(new[]
            {
                (WeaponType.LukeSkywalkerLaser, 1), (WeaponType.RedSquadronEscortLaser, luke.MemberCount - 1)
            }));
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            float dps = luke.WeaponLoadout.Sum(weapon => weapons.GetProfile(weapon.WeaponType).DamagePerSecond * weapon.Count);
            Assert.That(weapons.GetProfile(WeaponType.LukeSkywalkerLaser).DamagePerSecond,
                Is.GreaterThan(weapons.GetProfile(WeaponType.RedSquadronEscortLaser).DamagePerSecond));
            Assert.That(weapons.GetProfile(WeaponType.LukeSkywalkerLaser).DamageType, Is.EqualTo(DamageType.HeroLaser),
                "Luke must retain hero-versus-hero damage and accuracy instead of ordinary fighter penalties.");
            foreach (string guid in AssetDatabase.FindAssets("t:SquadronData", new[] { DATA_FOLDER }))
            {
                SquadronData other = AssetDatabase.LoadAssetAtPath<SquadronData>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == luke) continue;
                Assert.That(luke.TotalHull, Is.GreaterThan(other.TotalHull), other.name);
                Assert.That(luke.TotalShields, Is.GreaterThan(other.TotalShields), other.name);
                Assert.That(dps, Is.GreaterThan(other.WeaponLoadout.Sum(weapon =>
                    weapons.GetProfile(weapon.WeaponType).DamagePerSecond * weapon.Count)), other.name);
            }
        }

        [Test]
        public void SavedRoster_RegistersUniqueRebelHeroWithMatchingIconAndPreview()
        {
            FactionCatalog catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(
                "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset");
            Assert.That(catalog.Factions.Where(faction => faction.Squadrons.ContainsKey(SquadronType.LukeRedSquadron))
                .Select(faction => faction.FactionType), Is.EqualTo(new[] { FactionType.Rebellion }));
            FactionData entry = catalog.GetSquadronFactionData(SquadronType.LukeRedSquadron);
            Assert.That(entry.IsHero, Is.True);
            Assert.That(entry.MaxCount, Is.EqualTo(1));
            Assert.That(entry.AvailableLevel, Is.GreaterThan(0));
            Assert.That(entry.Price, Is.GreaterThan(0));
            Assert.That(entry.BuildTime, Is.GreaterThan(0));
            Assert.That(entry.UnitCapacity, Is.GreaterThan(0));
            Assert.That(entry.Icon, Is.Not.Null);
            Assert.That(entry.IconKey, Is.Not.Empty);
            ShipUiData ui = AssetDatabase.LoadAssetAtPath<ShipUiData>("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
            Assert.That(ui.GetSquadronIcon(SquadronType.LukeRedSquadron), Is.EqualTo(entry.Icon));
            ReinforcementData reinforcement = AssetDatabase.LoadAssetAtPath<ReinforcementData>(
                "Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
            Assert.That(reinforcement.GetSpawnPrefabKey(SquadronType.LukeRedSquadron), Is.EqualTo(AssetDatabase.AssetPathToGUID(PREVIEW)));
            AssetMappingData mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>("Assets/Settings/AssetMappingData.asset");
            Assert.That(mapping.GetAssetKey(UNIT + "SquadronView"), Is.EqualTo(AssetDatabase.AssetPathToGUID(VIEW)));
            Assert.That(mapping.GetAssetKey(UNIT + "SquadronData"), Is.EqualTo(AssetDatabase.AssetPathToGUID(DATA_FOLDER + UNIT + "SquadronData.asset")));
            foreach (string path in new[] { VIEW, PREVIEW })
            {
                var address = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
                Assert.That(address, Is.Not.Null, path);
                Assert.That(address.parentGroup.Name, Is.EqualTo("RebellionUnitViews"));
            }
        }

        [Test]
        public void SavedAbility_RequiresEnemyTargetAndHasRecovery()
        {
            Assert.That(LoadData().Abilities, Is.EqualTo(new[] { ShipAbilityId.LuckyShot }));
            ShipAbilityDefinition definition = AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>(
                "Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.LuckyShot);
            Assert.That(definition.RequiresEnemyTarget, Is.True);
            Assert.That(definition.RecoveryDelay, Is.GreaterThan(0f));
            Assert.That(definition.Icon, Is.Not.Null);
            Assert.That(definition.IsToggle, Is.False);
            Assert.That(definition.Settings, Is.TypeOf<LuckyShotSettings>());
            var settings = (LuckyShotSettings)definition.Settings;
            Assert.That(settings.Damage, Is.GreaterThan(0f));
            Assert.That(settings.Windup, Is.GreaterThanOrEqualTo(0f));
        }

        private static SquadronData LoadData() =>
            AssetDatabase.LoadAssetAtPath<SquadronData>(DATA_FOLDER + UNIT + "SquadronData.asset");
    }
}
