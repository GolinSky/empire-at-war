using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Utils;
using Newtonsoft.Json;
using UnityEngine;
using VContainer;

// Profiling only: invokes the actual read-only strength tally paths without ticking gameplay, editing views or running tests.
// run_script entry: ProfileCaptureGeometry.Run; args: [scope ObjectRef, output JSON path, passes (e.g.12)].
public static class ProfileCaptureGeometry
{
    public static object Main() => Run(UnityEngine.Object.FindAnyObjectByType<SkirmishScope>(),
        "Logs/LateBattleCpu/capture-geometry.json", 12);

    public static object Run(SkirmishScope scope, string outputPath, int passes)
    {
        if (passes < 1 || passes > 30) throw new ArgumentOutOfRangeException(nameof(passes));
        CaptureSitesSystem sites = scope.Container.Resolve<CaptureSitesSystem>();
        IShipService ships = scope.Container.Resolve<IShipService>();
        ISquadronRegistry squadrons = scope.Container.Resolve<ISquadronRegistry>();
        IPlayerRoster roster = scope.Container.Resolve<IPlayerRoster>();
        CaptureSiteData data = scope.Container.Resolve<CaptureSiteData>();
        int squadronCount = scope.Container.Resolve<IEntityLocator>().Entities.Count(entity =>
            entity.IsSquadron() && !entity.HealthModel.IsDestroyed);
        var builder = new CaptureStrengthBuilder(roster);
        var areas = sites.Sites.Cast<CaptureSitePresenter>().Select(site => new Area(site)).ToArray();
        var results = new List<object>();
        double baselineShips = 0, baselineSquadrons = 0, cachedShips = 0, cachedSquadrons = 0, snapshot = 0;

        foreach (Area area in areas)
        {
            Measure(area, false, ships, squadrons, data.SquadronCaptureWeight, builder);
            Measure(area, true, ships, squadrons, data.SquadronCaptureWeight, builder);
        }
        for (int pass = 0; pass < passes; pass++)
        for (int index = 0; index < areas.Length; index++)
        {
            Area area = areas[index];
            Timing baseline;
            Timing cached;
            if (pass % 2 == 0)
            {
                baseline = Measure(area, false, ships, squadrons, data.SquadronCaptureWeight, builder);
                cached = Measure(area, true, ships, squadrons, data.SquadronCaptureWeight, builder);
            }
            else
            {
                cached = Measure(area, true, ships, squadrons, data.SquadronCaptureWeight, builder);
                baseline = Measure(area, false, ships, squadrons, data.SquadronCaptureWeight, builder);
            }
            if (baseline.Strength.LeadingPlayer != cached.Strength.LeadingPlayer ||
                baseline.Strength.Advantage != cached.Strength.Advantage ||
                baseline.Strength.PresentTeamCount != cached.Strength.PresentTeamCount)
                throw new InvalidOperationException("Geometry snapshot changed capture strength at site " + index);
            baselineShips += baseline.ShipsMs;
            baselineSquadrons += baseline.SquadronsMs;
            cachedShips += cached.ShipsMs;
            cachedSquadrons += cached.SquadronsMs;
            snapshot += cached.SnapshotMs;
            results.Add(new { pass, site_index = index, kind = area.Site.Kind.ToString(),
                center = new { x = area.Center.x, y = area.Center.y, z = area.Center.z }, radius = area.Radius,
                baseline_ships_ms = baseline.ShipsMs, baseline_squadrons_ms = baseline.SquadronsMs,
                cached_snapshot_ms = cached.SnapshotMs, cached_ships_ms = cached.ShipsMs,
                cached_squadrons_ms = cached.SquadronsMs, parity = true });
        }

        var result = new
        {
            captured_at_utc = DateTime.UtcNow.ToString("O"),
            unity_frame = Time.frameCount,
            battle_seed = scope.Battle.Map.Seed,
            ships = ships.Ships.Count,
            squadrons = squadronCount,
            site_count = areas.Length,
            passes,
            synchronous = true,
            note = "Paired same-frame actual registry scans, alternating order after one warmup pair/site. Cached variant refreshes center/radius once per scan; includes snapshot cost. No gameplay tick, scene mutation or test execution. Microbenchmark is not a matched whole-frame speedup or attribution of the historical283ms spike.",
            baseline_ships_ms = baselineShips,
            baseline_squadrons_ms = baselineSquadrons,
            cached_ships_ms = cachedShips,
            cached_squadrons_ms = cachedSquadrons,
            cached_snapshot_ms = snapshot,
            all_strengths_identical = true,
            per_site_pass = results
        };
        string path = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { path, site_count = areas.Length, passes, baseline_ms = baselineShips + baselineSquadrons,
            cached_ms = cachedShips + cachedSquadrons + snapshot, parity = true };
    }

    private static Timing Measure(Area area, bool cached, IShipService ships, ISquadronRegistry squadrons,
        float weight, CaptureStrengthBuilder builder)
    {
        long start = Stopwatch.GetTimestamp();
        if (cached) area.Snapshot();
        double snapshotMs = MillisecondsSince(start);
        Func<Vector3, bool> contains = cached ? area.CachedContains : area.LiveContains;
        builder.Clear();
        start = Stopwatch.GetTimestamp();
        ships.AddShipStrength(contains, builder);
        double shipsMs = MillisecondsSince(start);
        start = Stopwatch.GetTimestamp();
        squadrons.AddSquadronStrength(contains, weight, builder);
        double squadronsMs = MillisecondsSince(start);
        return new Timing { SnapshotMs = snapshotMs, ShipsMs = shipsMs, SquadronsMs = squadronsMs,
            Strength = builder.Build() };
    }

    private static double MillisecondsSince(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

    private sealed class Area
    {
        public readonly CaptureSitePresenter Site;
        public readonly Func<Vector3, bool> LiveContains;
        public readonly Func<Vector3, bool> CachedContains;
        public Vector3 Center;
        public float Radius;

        public Area(CaptureSitePresenter site)
        {
            Site = site;
            LiveContains = position => Site.Contains(position);
            CachedContains = position => PlanarGeometry.ContainsCircle(position, Center, Radius);
        }

        public void Snapshot()
        {
            Center = Site.Center;
            Radius = Site.Radius;
        }
    }

    private struct Timing
    {
        public double SnapshotMs;
        public double ShipsMs;
        public double SquadronsMs;
        public CaptureStrength Strength;
    }
}
