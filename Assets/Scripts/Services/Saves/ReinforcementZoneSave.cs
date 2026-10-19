using System;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One reinforcement relay, matched by its index in the seeded map layout.</summary>
    [Serializable]
    public sealed class ReinforcementZoneSave
    {
        private const int NO_PLAYER = -1;

        [SerializeField] private int owner = NO_PLAYER;
        [SerializeField] private int capturingPlayer = NO_PLAYER;
        [SerializeField] private float captureProgress;
        [SerializeField] private bool isContested;

        public PlayerId Owner => owner == NO_PLAYER ? PlayerId.None : new PlayerId(owner);
        public PlayerId CapturingPlayer => capturingPlayer == NO_PLAYER ? PlayerId.None : new PlayerId(capturingPlayer);
        public float CaptureProgress => captureProgress;
        public bool IsContested => isContested;

        public static ReinforcementZoneSave Create(PlayerId owner, PlayerId capturingPlayer,
            float captureProgress, bool isContested) => new ReinforcementZoneSave
        {
            owner = owner.IsNone ? NO_PLAYER : owner.Index,
            capturingPlayer = capturingPlayer.IsNone ? NO_PLAYER : capturingPlayer.Index,
            captureProgress = captureProgress,
            isContested = isContested
        };
    }
}
