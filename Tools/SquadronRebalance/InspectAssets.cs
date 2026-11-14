using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEngine;

public static class InspectAssets
{
    public static object Inspect()
    {
        var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var rows = new List<object>();
        foreach (SquadronData data in WeaponLoadoutBaker.LoadAll<SquadronData>(WeaponLoadoutBaker.SQUADRON_DATA_FOLDER))
        {
            GameObject prefab = WeaponLoadoutBaker.LoadSquadronView(data);
            var components = BalancePrefabBindings.Components(prefab).ToArray();
            var bindings = components.OfType<EntityBindings>().Single();
            var fighters = BalancePrefabBindings.References(bindings.SquadronHealth, "fighters").Cast<FighterView>().ToArray();
            var mounts = bindings.Weapons.SelectMany(w => BalancePrefabBindings.References(w, "hardPoints")).Cast<WeaponHardPoint>().ToArray();
            var derived = WeaponLoadoutBaker.DeriveLoadout(prefab);
            var familyDps = mounts.GroupBy(m => weapons.GetProfile(m.WeaponType).Family).ToDictionary(g => g.Key.ToString(),
                g => g.Sum(m => (double)weapons.GetProfile(m.WeaponType).WithWarheadMultipliers(data.WarheadDamageMultiplier, data.WarheadReloadMultiplier).DamagePerSecond));
            rows.Add(new
            {
                unit = data.name, members = fighters.Length, bakedMembers = data.MemberCount,
                flightCount = bindings.SquadronFlight.Count,
                matchingLoadout = derived.Count == data.WeaponLoadout.Count && derived.All(e => data.WeaponLoadout.Any(d => d.WeaponType == e.WeaponType && d.Count == e.Count)),
                contiguousMemberIds = fighters.Select(f => f.Id).SequenceEqual(Enumerable.Range(0, fighters.Length)),
                uniqueMountIds = mounts.Select(m => m.Id).Distinct().Count() == mounts.Length,
                mounts = mounts.Length, data.MemberHull, data.MemberShields,
                shieldHpPerSecond = data.ShieldRegenerateValue / data.ShieldRegenerateDelay,
                data.HullRepairPerSecond, familyDps, totalDps = familyDps.Values.Sum(),
                torpedoVolley = mounts.Where(m => weapons.GetProfile(m.WeaponType).Family == WeaponFamily.FighterTorpedo)
                    .Sum(m => { var p = weapons.GetProfile(m.WeaponType); return (double)p.Damage * p.ShotsPerSalvo * data.WarheadDamageMultiplier; })
            });
        }
        return new { compilationFailed = EditorUtility.scriptCompilationFailed, compiling = EditorApplication.isCompiling, squadrons = rows };
    }
}
