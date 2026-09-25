using System;
using System.Collections.Generic;
using System.Numerics;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public class CinematicInterestScorer
    {
        private readonly CinematicCameraSettings _settings;
        private readonly Random _random;

        public CinematicInterestScorer(CinematicCameraSettings settings, Random random)
        {
            _settings = settings;
            _random = random;
        }

        public bool TrySelect(
            IReadOnlyList<CinematicCandidate> candidates,
            long previousTargetId,
            out CinematicSelection selection)
        {
            selection = default;
            float bestScore = float.NegativeInfinity;
            float engagementRadiusSquared = _settings.EngagementRadius * _settings.EngagementRadius;

            for (int i = 0; i < candidates.Count; i++)
            {
                CinematicCandidate candidate = candidates[i];
                int enemyCount = 0;
                Vector3 enemySum = Vector3.Zero;

                for (int j = 0; j < candidates.Count; j++)
                {
                    CinematicCandidate other = candidates[j];
                    if (!AreEnemies(candidate.PlayerType, other.PlayerType) ||
                        Vector3.DistanceSquared(candidate.Position, other.Position) > engagementRadiusSquared)
                    {
                        continue;
                    }

                    enemyCount++;
                    enemySum += other.Position;
                }

                float score = Score(candidate, enemyCount, previousTargetId);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                Vector3 focusOffset = enemyCount == 0
                    ? Vector3.Zero
                    : (candidate.Position + enemySum) / (enemyCount + 1) - candidate.Position;
                selection = new CinematicSelection(candidate, focusOffset);
            }

            return candidates.Count > 0;
        }

        private float Score(CinematicCandidate candidate, int enemyCount, long previousTargetId)
        {
            float activity = Math.Clamp(
                1f - candidate.SecondsSinceDamaged / _settings.DamageMemorySeconds, 0f, 1f);
            float score = _settings.GetClassProfile(candidate.ShipClass).InterestWeight +
                          _settings.DamageWeight * activity +
                          _settings.NearbyEnemyWeight * Math.Min(enemyCount, _settings.MaxCountedEnemies);

            if (candidate.Id == previousTargetId)
            {
                score *= _settings.RepeatTargetPenalty;
            }

            return score * (1f + _settings.ScoreJitter * (float)_random.NextDouble());
        }

        private static bool AreEnemies(PlayerType first, PlayerType second)
        {
            return first != second && first != PlayerType.None && second != PlayerType.None;
        }
    }
}
