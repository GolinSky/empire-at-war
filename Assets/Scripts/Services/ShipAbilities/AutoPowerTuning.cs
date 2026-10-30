using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    [Serializable]
    public sealed class AutoPowerTuning
    {
        [SerializeField] private float decisionInterval = 0.5f;
        [SerializeField] private int castersPerFrame = 4;
        [SerializeField] private float shieldLossWindow = 3f;
        [SerializeField] private float heavyShieldLoss = 0.25f;
        [SerializeField] private float lowShields = 0.6f;
        [SerializeField] private float lastStandHull = 0.35f;
        [SerializeField] private int confirmationChecks = 2;
        [SerializeField] private float manualActionDelay = 2f;
        [SerializeField] private float lastStandForceRatio = 1.5f;
        // Force is current profile DPS against the caster's class. A corvette's DPS is one unit.
        [SerializeField] private float minimumCorvetteForce = 0.5f;

        public float DecisionInterval => decisionInterval;
        public int CastersPerFrame => castersPerFrame;
        public float ShieldLossWindow => shieldLossWindow;
        public float HeavyShieldLoss => heavyShieldLoss;
        public float LowShields => lowShields;
        public float LastStandHull => lastStandHull;
        public int ConfirmationChecks => confirmationChecks;
        public float ManualActionDelay => manualActionDelay;
        public float LastStandForceRatio => lastStandForceRatio;
        public float MinimumCorvetteForce => minimumCorvetteForce;
    }
}
