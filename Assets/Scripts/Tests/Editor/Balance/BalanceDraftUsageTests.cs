using System;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BalanceDraftUsageTests
    {
        [Test]
        public void WeaponDraft_UpdatesConsumersAndCompareFieldsThenUndoRestoresThem()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceUnit unit = registry.Units.First(entry => entry.Id == "Separatist/ships/101");
            var mounts = registry.Fields.Values.Where(field => !field.SharedMountSource && field.Stat == "WeaponType" && field.Users.Contains(unit)).ToList();
            string oldId = mounts[0].Read();
            BalanceField oldProfile = registry.Fields.Values.Single(field => field.Stat == "weapon/" + oldId + "/damage");
            BalanceField newProfile = registry.Fields.Values.First(field => field.Stat.StartsWith("weapon/") && field.Stat.EndsWith("/damage") && !field.Users.Contains(unit));
            string schema = newProfile.Schema;
            BalanceDraft draft = new BalanceDraft();
            var replaced = mounts.Where(field => field.Read() == oldId).ToList();
            draft.SetMany(replaced.Select(field => field.Snapshot()).ToList(), replaced.Select(_ => newProfile.Stat.Split('/')[1]).ToList());
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, draft);
            Assert.That(usage.Users(oldProfile).Contains(unit), Is.False);
            Assert.That(usage.Users(newProfile).Contains(unit), Is.True);
            Assert.That(usage.Fields(unit).ContainsKey(oldProfile.Stat), Is.False);
            Assert.That(usage.Fields(unit)[newProfile.Stat], Is.EqualTo(newProfile.Key));
            Assert.That(newProfile.Schema, Is.EqualTo(schema), "Preview must not replace the live conflict baseline.");
            Assert.That(replaced.All(field => field.Read() == oldId), Is.True);
            draft.Undo();
            usage = new BalanceDraftUsage(registry, draft);
            Assert.That(usage.Users(oldProfile).Contains(unit), Is.True);
            Assert.That(usage.Users(newProfile).Contains(unit), Is.False);
        }

        [Test]
        public void AbilityDraft_UpdatesAliasedSettingsAndDefinitionConsumers()
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceField settings = registry.Fields.Values.First(field => field.Stat.StartsWith("settings/") && field.Stat.Split('/')[1].Contains('+'));
            string id = settings.Stat.Split('/')[1].Split('+')[0];
            BalanceField definition = registry.Fields.Values.Single(field => field.Stat == "ability/" + id + "/duration");
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == "Ship" && !settings.Users.Contains(entry));
            BalanceField assignment = registry.Fields[unit.Fields["Abilities"]];
            string before = assignment.Read();
            BalanceDraft draft = new BalanceDraft(); draft.Set(assignment.Snapshot(), id);
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, draft);
            Assert.That(usage.Users(settings).Contains(unit), Is.True);
            Assert.That(usage.Users(definition).Contains(unit), Is.True);
            Assert.That(usage.Fields(unit)[settings.Stat], Is.EqualTo(settings.Key));
            Assert.That(assignment.Read(), Is.EqualTo(before));
            draft.Set(assignment.Snapshot(), "");
            usage = new BalanceDraftUsage(registry, draft);
            Assert.That(usage.Users(settings).Contains(unit), Is.False);
            Assert.That(usage.Fields(unit).ContainsKey(settings.Stat), Is.False);
        }

        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        public void MissingWeaponProfile_ShowsErrorAndOffersOnlyRegisteredChoices(int id)
        {
            BalanceRegistration registry = BalanceInventory.Build();
            Assert.That(registry.Errors, Is.Empty);
            BalanceUnit unit = registry.Units.First(entry => entry.Kind == "Ship");
            BalanceField mount = registry.Fields.Values.First(field => !field.SharedMountSource && field.Stat == "WeaponType" && field.Users.Contains(unit));
            BalanceWindowState state = new BalanceWindowState(); state.Draft.Set(mount.Snapshot(), id.ToString());
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, state.Draft);
            string summary = BalanceUnitView.BaseDps(unit, registry, state.Draft);
            Assert.That(summary, Does.Contain("Missing weapon profile"));
            VisualElement card = BalanceFieldView.Create(mount, state, usage, _ => { }, () => { });
            Assert.That(card.Q<HelpBox>().text, Does.Contain("Missing weapon profile"));
            Assert.That(card.Q<DropdownField>().choices, Does.Not.Contain(Enum.GetName(mount.EnumType, id)));
            Assert.That(card.Q<DropdownField>().choices.Count, Is.EqualTo(usage.WeaponIds.Count));
            Assert.That(BalanceValidation.Preflight(registry, state.Draft), Has.Some.Contains("Weapon profile missing"));
        }
    }
}
