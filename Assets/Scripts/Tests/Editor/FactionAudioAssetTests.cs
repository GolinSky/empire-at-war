using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    public sealed class FactionAudioAssetTests
    {
        private const string SHIP_DATA_PATH = "Assets/Settings/Data/Models/Audio/ShipSfxData.asset";

        [TestCase(ShipAbilityId.Invulnerability)]
        [TestCase(ShipAbilityId.BoostShieldPower)]
        [TestCase(ShipAbilityId.NebulonBBoostShieldStrength)]
        [TestCase(ShipAbilityId.MonCalCruiserPowerToShields)]
        [TestCase(ShipAbilityId.HomeOnePowerToShields)]
        [TestCase(ShipAbilityId.MC80IndependencePowerToShields)]
        [TestCase(ShipAbilityId.CorellianBattlecruiserPowerToShields)]
        [TestCase(ShipAbilityId.AssaultFrigateMkIIPowerToShields)]
        public void ShieldAbilitiesUseShieldCues(ShipAbilityId ability)
        {
            var profile = Data().GetAbility(ability);
            AssertClip(profile.Start, "GUI_SHIELDS_UP_1", 0.25f);
            AssertClip(profile.End, "GUI_SHIELDS_DOWN_1", 0.25f);
        }

        [TestCase(ShipAbilityId.BoostEnginePower)]
        [TestCase(ShipAbilityId.CorellianCorvettePowerToEngines)]
        [TestCase(ShipAbilityId.ImperialBoostEnginePower)]
        [TestCase(ShipAbilityId.CD273PowerToEngines)]
        [TestCase(ShipAbilityId.LockSFoils)]
        public void EngineAndFoilAbilitiesUseToggleCues(ShipAbilityId ability)
        {
            var profile = Data().GetAbility(ability);
            AssertClip(profile.Start, "GUI_TOGGLE_17", 0.15f);
            AssertClip(profile.End, "GUI_TOGGLE_18", 0.15f);
        }

        [TestCase(ShipAbilityId.BoostWeaponPower)]
        [TestCase(ShipAbilityId.VictoryBoostWeaponPower)]
        [TestCase(ShipAbilityId.TIEAvengerPowerToWeapons)]
        [TestCase(ShipAbilityId.ArquitensBoostWeaponPower)]
        [TestCase(ShipAbilityId.AcclamatorBoostWeaponPower)]
        [TestCase(ShipAbilityId.TectorBoostWeaponPower)]
        [TestCase(ShipAbilityId.NebulaOverchargeWeapons)]
        public void WeaponBoostAbilitiesUseApprovedActivation(ShipAbilityId ability) =>
            AssertClip(Data().GetAbility(ability).Start, "GUI_TOGGLE_UW_2", 0.15f);

        [Test]
        public void SensorJammingUsesItsDedicatedActivation() =>
            AssertClip(Data().GetAbility(ShipAbilityId.DauntlessSensorJamming).Start, "GUI_Toggle_UW_3", 0.15f);

        [Test]
        public void UnitReadyUsesEventMappingDespiteMisleadingFilenamePrefixes()
        {
            var data = Data();
            Assert.That(data.GetUnit(ShipType.ISDI).Ready.name, Is.EqualTo("U000_VSD0302_ENG"));
            Assert.That(data.GetUnit(ShipType.Victory).Ready.name, Is.EqualTo("U000_STD0301_ENG"));
            Assert.That(data.GetUnit(ShipType.MonCalCruiser).Ready.name, Is.EqualTo("U000_MCC0308_ENG"));
            Assert.That(data.GetUnit(ShipType.Venator).Ready, Is.Null, "Republic uses faction construction fallback.");
            Assert.That(data.GetUnit(ShipType.Providence).Ready, Is.Null, "Separatist uses faction construction fallback.");
        }

        [Test]
        public void ImportedVoiceClipsUseCompressedMonoAndAppropriatePreload()
        {
            foreach (string folder in new[] { "Announcer", "UnitReady" })
            {
                string[] assets = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Voices/" + folder });
                Assert.That(assets.Length, Is.GreaterThan(0));
                foreach (string guid in assets)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    Assert.That(importer.forceToMono, Is.True, path);
                    Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.CompressedInMemory), path);
                    Assert.That(importer.defaultSampleSettings.quality, Is.EqualTo(0.5f).Within(0.001f), path);
                    Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.EqualTo(folder == "Announcer"), path);
                    Assert.That(importer.loadInBackground, Is.EqualTo(folder == "UnitReady"), path);
                }
            }
        }

        private static ShipSfxData Data() => AssetDatabase.LoadAssetAtPath<ShipSfxData>(SHIP_DATA_PATH);

        private static void AssertClip(SfxProfile profile, string clipName, float minimumCooldown)
        {
            Assert.That(profile.HasClip, Is.True);
            Assert.That(profile.GetClip().name, Is.EqualTo(clipName));
            Assert.That(profile.Cooldown, Is.GreaterThanOrEqualTo(minimumCooldown));
        }
    }
}
