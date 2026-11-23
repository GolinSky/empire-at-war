using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Plan-vs-actual loss exchange of one AI player against one hostile team (W67). At each fight start the AI records
    /// the value it expects each side to lose (<see cref="CombatMatchup.PredictLossShares"/>); during the fight it
    /// records the value each side really lost (unit prices). <see cref="GetCorrection"/> is the actual exchange over the
    /// predicted one: above 1 after better trades than rated (braver), below 1 after worse ones (more careful).
    /// Bounded to [<see cref="MIN_CORRECTION"/>, <see cref="MAX_CORRECTION"/>]; every record fades with
    /// <see cref="MEMORY_SECONDS"/>, so the correction drifts back to 1 without new fights.
    /// </summary>
    public sealed class EnemyTradeLedger
    {
        public const float MIN_CORRECTION = 0.5f;
        public const float MAX_CORRECTION = 2f;
        /// <summary>Time constant of the fade: trades of the last few minutes speak for the current fleets.</summary>
        public const float MEMORY_SECONDS = 240f;
        /// <summary>Credits of even exchange every ratio starts from (about two frigates), so one early loss swings little.</summary>
        public const float PRIOR_VALUE = 5000f;

        private float _predictedOwn;
        private float _predictedHostile;
        private float _actualOwn;
        private float _actualHostile;
        private float _lastTime = float.NaN;

        /// <summary>
        /// Actual over predicted exchange (hostile value lost per own value lost) at <paramref name="now"/>; 1 without
        /// records. Multiply a rating by it: a fight rated 0.6 by strength reads 0.9 after trades 1.5 times better than
        /// predicted.
        /// </summary>
        public float GetCorrection(float now)
        {
            float keep = GetKeep(now);
            float actual = (keep * (_actualHostile + _predictedHostile) + PRIOR_VALUE) /
                (keep * (_actualOwn + _predictedOwn) + PRIOR_VALUE);
            float predicted = (keep * _predictedHostile + PRIOR_VALUE) / (keep * _predictedOwn + PRIOR_VALUE);
            return Math.Min(MAX_CORRECTION, Math.Max(MIN_CORRECTION, actual / predicted));
        }

        /// <summary>Records the losses a fight at <paramref name="advantage"/> is expected to cost each side.</summary>
        /// <param name="ownValue">Price of the own units in the fight.</param>
        /// <param name="hostileValue">Price of the hostile units in the fight.</param>
        public void RecordPrediction(float advantage, float ownValue, float hostileValue, float now)
        {
            Fade(now);
            (float own, float hostile) = CombatMatchup.PredictLossShares(advantage);
            _predictedOwn += own * ownValue;
            _predictedHostile += hostile * hostileValue;
        }

        /// <summary>Records a unit lost in a fight against this team, at its price.</summary>
        public void RecordLoss(bool own, float value, float now)
        {
            Fade(now);
            if (own) _actualOwn += value;
            else _actualHostile += value;
        }

        /// <summary>
        /// <see cref="CombatMatchup.ExpectedGain"/> at <paramref name="advantage"/> corrected by this ledger's trades:
        /// what attacking a target is expected to win (hostile value destroyed + objective value) minus own value lost.
        /// </summary>
        public float ExpectedGain(float advantage, float ownValue, float hostileValue, float objectiveValue, float now) =>
            CombatMatchup.ExpectedGain(advantage * GetCorrection(now), ownValue, hostileValue, objectiveValue);

        private void Fade(float now)
        {
            float keep = GetKeep(now);
            _predictedOwn *= keep;
            _predictedHostile *= keep;
            _actualOwn *= keep;
            _actualHostile *= keep;
            if (float.IsNaN(_lastTime) || now > _lastTime) _lastTime = now;
        }

        private float GetKeep(float now) => float.IsNaN(_lastTime) || now <= _lastTime ? 1f :
            (float)Math.Exp(-(now - _lastTime) / MEMORY_SECONDS);
    }
}
