using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Host: sends pause and speed (the host's <c>Time.timeScale</c>) whenever it changes after the battle begins.</summary>
    public sealed class HostMatchStateSender : ITickable
    {
        private readonly OnlineMatchService _online;
        private readonly NetworkTickService _ticks;
        private float _sentScale = -1f;

        public HostMatchStateSender(OnlineMatchService online, NetworkTickService ticks)
        {
            _online = online;
            _ticks = ticks;
        }

        public void Tick()
        {
            if (!_online.HasBegun || Mathf.Approximately(_sentScale, Time.timeScale)) return;
            _sentScale = Time.timeScale;
            HostBroadcast.ToClients(_online, new MatchStateMessage { Tick = _ticks.CurrentTick, TimeScale = _sentScale });
        }
    }
}
