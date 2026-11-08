using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host: every <see cref="SEND_INTERVAL_SECONDS"/> captures each <see cref="IReplicatedState"/> and sends the ones
    /// whose JSON changed to their audience. Joining clients get every state in their baseline.
    /// </summary>
    public sealed class HostReplicatedStateSender : ITickable
    {
        private const float SEND_INTERVAL_SECONDS = 0.5f;

        private readonly ReplicatedStateRegistry _states;
        private readonly OnlineMatchService _online;
        private readonly Dictionary<string, string> _sent = new Dictionary<string, string>();
        private float _timer;

        public HostReplicatedStateSender(ReplicatedStateRegistry states, OnlineMatchService online)
        {
            _states = states;
            _online = online;
        }

        public void Tick()
        {
            if (!_online.HasBegun) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = SEND_INTERVAL_SECONDS;
            foreach (IReplicatedState state in _states.States)
            {
                string json = state.Capture();
                if (_sent.TryGetValue(state.Key, out string sent) && sent == json) continue;
                _sent[state.Key] = json;
                ReplicatedStateMessage message = new ReplicatedStateMessage { Key = state.Key, Json = json };
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (IsAudience(connection, state.Audience)) connection.Send(message);
                }
            }
        }

        public void SendBaseline(NetworkConnectionToClient connection)
        {
            foreach (IReplicatedState state in _states.States)
            {
                if (IsAudience(connection, state.Audience))
                    connection.Send(new ReplicatedStateMessage { Key = state.Key, Json = state.Capture() });
            }
        }

        // The host's own connection runs the simulation and needs nothing; a loading client has no handlers yet.
        private bool IsAudience(NetworkConnectionToClient connection, PlayerId audience)
        {
            if (!_online.IsBattleLoaded(connection)) return false;
            if (audience == PlayerId.None) return true;
            return _online.TryGetPlayer(connection.connectionId, out PlayerId player) && player == audience;
        }
    }
}
