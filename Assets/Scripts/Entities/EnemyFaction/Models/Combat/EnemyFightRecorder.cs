using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Feeds one group's fights into its player's <see cref="EnemyTradeLedger"/>s (W67). A fight starts when hostiles
    /// first stand near the group: the predicted losses go to the ledger of that hostile team. While it lasts, the
    /// group's lost ships count against that team and destroyed hostile ships seen in it count for it. It ends after
    /// <see cref="FIGHT_END_SECONDS"/> without hostiles near.
    /// </summary>
    public sealed class EnemyFightRecorder
    {
        /// <summary>A pause this long without hostiles near the group ends the fight; a shorter one is the same fight.</summary>
        public const float FIGHT_END_SECONDS = 10f;

        private readonly Func<TeamId, EnemyTradeLedger> _ledgers;
        private readonly Dictionary<long, (TeamId Team, float Value)> _hostiles =
            new Dictionary<long, (TeamId Team, float Value)>();
        private readonly List<long> _resolved = new List<long>();
        private TeamId _team;
        private float _lastContact = float.NegativeInfinity;

        public bool IsActive { get; private set; }

        public EnemyFightRecorder(Func<TeamId, EnemyTradeLedger> ledgers)
        {
            _ledgers = ledgers;
        }

        /// <summary>Remembers a hostile ship near the group while a fight runs, so its death counts for this group.</summary>
        public void TrackHostile(long entityId, TeamId team, float value)
        {
            if (IsActive) _hostiles[entityId] = (team, value);
        }

        /// <summary>One of the group's ships was lost; it counts only inside a fight.</summary>
        public void RecordOwnLoss(float value, float now)
        {
            if (IsActive) _ledgers(_team).RecordLoss(true, value, now);
        }

        /// <summary>
        /// Advances the fight. <paramref name="inFight"/>: hostiles stand near the group now. A new fight records the
        /// losses predicted at <paramref name="advantage"/> for <paramref name="ownValue"/> against
        /// <paramref name="hostileValue"/> in the ledger of <paramref name="hostileTeam"/>.
        /// </summary>
        /// <param name="isDestroyed">True once a tracked hostile is destroyed or gone.</param>
        public void Update(float now, bool inFight, TeamId hostileTeam, float advantage, float ownValue,
            float hostileValue, Func<long, bool> isDestroyed)
        {
            ResolveHostileDeaths(isDestroyed, now);
            if (inFight)
            {
                if (!IsActive)
                {
                    IsActive = true;
                    _team = hostileTeam;
                    _ledgers(hostileTeam).RecordPrediction(advantage, ownValue, hostileValue, now);
                }
                _lastContact = now;
                return;
            }
            if (!IsActive || now - _lastContact < FIGHT_END_SECONDS) return;
            IsActive = false;
            _hostiles.Clear();
        }

        private void ResolveHostileDeaths(Func<long, bool> isDestroyed, float now)
        {
            _resolved.Clear();
            foreach (KeyValuePair<long, (TeamId Team, float Value)> hostile in _hostiles)
            {
                if (!isDestroyed(hostile.Key)) continue;
                _ledgers(hostile.Value.Team).RecordLoss(false, hostile.Value.Value, now);
                _resolved.Add(hostile.Key);
            }
            foreach (long id in _resolved) _hostiles.Remove(id);
        }
    }
}
