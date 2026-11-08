using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>
    /// Survival wave, phase and preparation countdown (whole seconds, so it changes at most once per second). Clients
    /// render the HUD and reach Survival victory from it.
    /// </summary>
    public sealed class SurvivalReplicatedState : ReplicatedState
    {
        private readonly SurvivalModel _model;

        public SurvivalReplicatedState(ReplicatedStateRegistry registry, IMatchSession session, SurvivalModel model)
            : base(registry, session)
        {
            _model = model;
        }

        public override string Key => "survival";

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            WaveIndex = _model.WaveIndex,
            Phase = _model.Phase,
            PreparationSeconds = Mathf.CeilToInt(_model.PreparationTimeLeft)
        });

        public override void Apply(string json)
        {
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(json);
            _model.Restore(snapshot.WaveIndex, snapshot.Phase, snapshot.PreparationSeconds);
        }

        // SurvivalMode already leaves the wave director unticked on clients.
        protected override void FollowHost()
        {
        }

        [Serializable]
        private sealed class Snapshot
        {
            public int WaveIndex;
            public SurvivalPhase Phase;
            public int PreparationSeconds;
        }
    }
}
