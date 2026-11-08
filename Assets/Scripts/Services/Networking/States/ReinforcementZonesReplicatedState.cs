using System;
using System.Linq;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.States
{
    /// <summary>Reinforcement relay owner and capture progress, in map layout order (as saves).</summary>
    public sealed class ReinforcementZonesReplicatedState : ReplicatedState
    {
        private readonly ReinforcementZonesSystem _zones;

        public ReinforcementZonesReplicatedState(ReplicatedStateRegistry registry, IMatchSession session,
            ReinforcementZonesSystem zones) : base(registry, session)
        {
            _zones = zones;
        }

        public override string Key => "reinforcement-zones";

        public override string Capture() => JsonUtility.ToJson(new Snapshot
        {
            Zones = _zones.Zones.Select(zone => ReinforcementZoneSave.Create(zone.Owner, zone.CapturingPlayer,
                zone.CaptureProgress, zone.IsContested)).ToArray()
        });

        public override void Apply(string json) => _zones.RestoreZones(JsonUtility.FromJson<Snapshot>(json).Zones);

        protected override void FollowHost() => _zones.FollowHost();

        [Serializable]
        private sealed class Snapshot
        {
            public ReinforcementZoneSave[] Zones;
        }
    }
}
