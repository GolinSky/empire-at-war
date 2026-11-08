using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Registers with <see cref="ReplicatedStateRegistry"/>; on an online client it also tells its system to stop
    /// simulating (<see cref="FollowHost"/>), since the host's state replaces it.
    /// </summary>
    public abstract class ReplicatedState : IReplicatedState, IInitializable, ILateDisposable
    {
        private readonly ReplicatedStateRegistry _registry;
        private readonly IMatchSession _session;

        protected ReplicatedState(ReplicatedStateRegistry registry, IMatchSession session)
        {
            _registry = registry;
            _session = session;
        }

        public abstract string Key { get; }

        public virtual PlayerId Audience => PlayerId.None;

        public void Initialize()
        {
            if (!_session.IsAuthority) FollowHost();
            _registry.Register(this);
        }

        public void LateDispose() => _registry.Unregister(this);

        public abstract string Capture();

        public abstract void Apply(string json);

        protected abstract void FollowHost();
    }
}
