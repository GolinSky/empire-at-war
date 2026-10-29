using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Models.Health
{
    public sealed class HardPointHealthBudget
    {
        public const float DEFAULT_BUDGET_RATIO = 1.15f;

        private readonly float _budgetRatio;
        private readonly float[] _typeWeights;
        private readonly float[] _classMinimums;

        public static float[] DefaultTypeWeights => new[] { 1f, 1.5f, 1.5f, 1f, 1.2f, 1.2f, 1.2f, 1.2f, 0.7f, 0.7f, 1.2f, 1.2f };
        public static float[] DefaultClassMinimums => new[] { 0f, 0f, 100f, 150f, 250f, 300f, 0f, 0f, 200f };

        public HardPointHealthBudget() : this(DEFAULT_BUDGET_RATIO, DefaultTypeWeights, DefaultClassMinimums) { }

        public HardPointHealthBudget(float budgetRatio, float[] typeWeights, float[] classMinimums)
        {
            _budgetRatio = budgetRatio;
            _typeWeights = (float[])typeWeights.Clone();
            _classMinimums = (float[])classMinimums.Clone();
        }

        public float[] Calculate(float maxHull, ShipClass shipClass, IReadOnlyList<HardPointModel> hardPoints, int level = 1)
        {
            var health = new float[hardPoints.Count];
            float totalWeight = 0f;
            for (int i = 0; i < hardPoints.Count; i++)
            {
                HardPointModel point = hardPoints[i];
                if (point.UnlockLevel > level) continue;
                float weight = _typeWeights[(int)point.HardPointType] * point.HealthWeight;
                if (!float.IsFinite(weight) || weight <= 0f)
                    throw new InvalidOperationException($"Hardpoint {point.Id} requires a positive finite health weight.");
                health[i] = weight;
                totalWeight += weight;
            }

            float minimum = _classMinimums[(int)shipClass];
            if (!float.IsFinite(_budgetRatio) || _budgetRatio < 1f || !float.IsFinite(minimum) || minimum < 0f)
                throw new InvalidOperationException("Hardpoint health requires a budget ratio >= 1 and a nonnegative finite class minimum.");
            for (int i = 0; i < health.Length; i++)
                if (hardPoints[i].UnlockLevel <= level)
                    health[i] = Math.Max(minimum, maxHull * _budgetRatio * (health[i] / totalWeight));
            return health;
        }
    }
}
