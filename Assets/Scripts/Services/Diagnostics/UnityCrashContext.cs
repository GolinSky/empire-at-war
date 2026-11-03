using System.Globalization;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Versioning;
using UnityEngine;
using UnityEngine.CrashReportHandler;

namespace EmpireAtWar.Services.Diagnostics
{
    public sealed class UnityCrashContext : ICrashContext
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeCapture()
        {
#if UNITY_EDITOR
            CrashReportHandler.enableCaptureExceptions = false;
#else
            CrashReportHandler.enableCaptureExceptions = true;
            BuildInfo build = BuildInfo.LoadCurrent();
            CrashReportHandler.SetUserMetadata("build_version", build.Version);
            CrashReportHandler.SetUserMetadata("build_number", build.Build);
            CrashReportHandler.SetUserMetadata("build_commit", build.Commit);
            // A report label, not UGS environment routing: Diagnostics is independent of UGS initialization.
            CrashReportHandler.SetUserMetadata("build_channel", Debug.isDebugBuild ? "development" : "production");
            new UnityCrashContext().EndBattle();
#endif
        }

        public void BeginBattle(GameMode mode, MapType map, PlanetType planet, FactionType faction)
        {
#if !UNITY_EDITOR
            CrashReportHandler.SetUserMetadata("game_mode", mode.ToString());
            CrashReportHandler.SetUserMetadata("map_type", map.ToString());
            CrashReportHandler.SetUserMetadata("planet", planet.ToString());
            CrashReportHandler.SetUserMetadata("player_faction", faction.ToString());
#endif
        }

        public void UpdateBattle(BattleState state, float seconds, int unitCount)
        {
#if !UNITY_EDITOR
            CrashReportHandler.SetUserMetadata("battle_state", state.ToString());
            CrashReportHandler.SetUserMetadata("battle_seconds", seconds.ToString("F0", CultureInfo.InvariantCulture));
            CrashReportHandler.SetUserMetadata("unit_count", unitCount.ToString(CultureInfo.InvariantCulture));
#endif
        }

        public void EndBattle()
        {
#if !UNITY_EDITOR
            CrashReportHandler.SetUserMetadata("game_mode", "none");
            CrashReportHandler.SetUserMetadata("map_type", "none");
            CrashReportHandler.SetUserMetadata("planet", "none");
            CrashReportHandler.SetUserMetadata("player_faction", "none");
            CrashReportHandler.SetUserMetadata("battle_state", "none");
            CrashReportHandler.SetUserMetadata("battle_seconds", "0");
            CrashReportHandler.SetUserMetadata("unit_count", "0");
#endif
        }
    }
}
