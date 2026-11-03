using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>
    /// Unity Gaming Services adapter. Nothing is collected until the player grants AnalyticsIntent consent.
    /// Initialization throws when the project is not linked to a UGS project.
    /// </summary>
    public sealed class UgsAnalyticsService : Service, IAnalyticsService, IInitializable
    {
        private const string PRODUCTION_ENVIRONMENT = "production";
        private const string DEVELOPMENT_ENVIRONMENT = "development";

        public async void Initialize()
        {
            InitializationOptions options = new InitializationOptions()
                .SetEnvironmentName(Debug.isDebugBuild ? DEVELOPMENT_ENVIRONMENT : PRODUCTION_ENVIRONMENT);
            await UnityServices.InitializeAsync(options);
        }

        public void RecordMatchStarted(MatchStartedData data)
        {
            UgsAnalyticsEvent analyticsEvent = new UgsAnalyticsEvent("match_started");
            analyticsEvent.Set("match_id", data.MatchId);
            analyticsEvent.Set("game_mode", data.Mode.ToString());
            analyticsEvent.Set("map_type", data.MapType.ToString());
            analyticsEvent.Set("planet", data.Planet.ToString());
            analyticsEvent.Set("map_size", data.MapSize.ToString());
            analyticsEvent.Set("victory_condition", data.VictoryCondition.ToString());
            analyticsEvent.Set("player_faction", data.PlayerFaction.ToString());
            analyticsEvent.Set("opponent_factions", JoinFactions(data.OpponentFactions));
            analyticsEvent.Set("ai_count", data.AiCount);
            analyticsEvent.Set("starting_money", data.StartingMoney);
            analyticsEvent.Set("initial_station_level", data.InitialStationLevel);
            analyticsEvent.Set("is_loaded_save", data.IsLoadedSave);
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

        public void RecordMatchCompleted(MatchCompletedData data)
        {
            UgsAnalyticsEvent analyticsEvent = new UgsAnalyticsEvent("match_completed");
            analyticsEvent.Set("match_id", data.MatchId);
            analyticsEvent.Set("game_mode", data.Mode.ToString());
            analyticsEvent.Set("result", ToResult(data.Outcome));
            analyticsEvent.Set("victory_condition", data.VictoryCondition.ToString());
            analyticsEvent.Set("player_faction", data.PlayerFaction.ToString());
            analyticsEvent.Set("opponent_factions", JoinFactions(data.OpponentFactions));
            analyticsEvent.Set("duration_seconds", data.DurationSeconds);
            analyticsEvent.Set("player_ships_left", data.PlayerShipsLeft);
            analyticsEvent.Set("enemy_ships_left", data.EnemyShipsLeft);
            analyticsEvent.Set("player_base_alive", data.IsPlayerBaseAlive);
            analyticsEvent.Set("enemy_base_alive", data.IsEnemyBaseAlive);
            analyticsEvent.Set("units_destroyed", data.UnitsDestroyed);
            analyticsEvent.Set("units_lost", data.UnitsLost);
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

        public void RecordMatchAbandoned(MatchAbandonedData data)
        {
            UgsAnalyticsEvent analyticsEvent = new UgsAnalyticsEvent("match_abandoned");
            analyticsEvent.Set("match_id", data.MatchId);
            analyticsEvent.Set("game_mode", data.Mode.ToString());
            analyticsEvent.Set("duration_seconds", data.DurationSeconds);
            analyticsEvent.Set("reason", data.Reason.ToString());
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

        public void RecordSurvivalWaveCompleted(SurvivalWaveCompletedData data)
        {
            UgsAnalyticsEvent analyticsEvent = new UgsAnalyticsEvent("survival_wave");
            analyticsEvent.Set("match_id", data.MatchId);
            analyticsEvent.Set("wave_index", data.WaveIndex);
            analyticsEvent.Set("battle_seconds", data.BattleSeconds);
            analyticsEvent.Set("player_ships_left", data.PlayerShipsLeft);
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

        public void RecordPerformanceSample(PerformanceSampleData data)
        {
            UgsAnalyticsEvent analyticsEvent = new UgsAnalyticsEvent("performance_sample");
            analyticsEvent.Set("match_id", data.MatchId);
            analyticsEvent.Set("avg_fps", data.AverageFps);
            analyticsEvent.Set("p5_fps", data.LowFps);
            analyticsEvent.Set("memory_mb", data.MemoryMb);
            analyticsEvent.Set("unit_count", data.UnitCount);
            analyticsEvent.Set("quality_level", data.QualityLevel);
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

        public void Flush() => AnalyticsService.Instance.Flush();

        public void SetConsent(bool isGranted)
        {
            ConsentState state = EndUserConsent.GetConsentState();
            ConsentStatus status = isGranted ? ConsentStatus.Granted : ConsentStatus.Denied;
            if (state.AnalyticsIntent == status) return;

            bool isRevoking = state.AnalyticsIntent == ConsentStatus.Granted;
            state.AnalyticsIntent = status;
            EndUserConsent.SetConsentState(state);
            // Deletion needs the initialized SDK; a revoke applied at boot before initialization sends no request.
            if (isRevoking && UnityServices.State == ServicesInitializationState.Initialized)
                AnalyticsService.Instance.RequestDataDeletion();
        }

        // UGS has no array parameters; sorting keeps one value per opponent mix.
        private static string JoinFactions(IReadOnlyList<FactionType> factions) =>
            string.Join(",", factions.Select(faction => faction.ToString()).OrderBy(name => name));

        private static string ToResult(BattleOutcome outcome) => outcome switch
        {
            BattleOutcome.PlayerVictory => "win",
            BattleOutcome.EnemyVictory => "loss",
            BattleOutcome.Draw => "draw",
            _ => throw new System.ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }
}
