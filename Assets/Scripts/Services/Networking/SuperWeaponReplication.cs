using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.SuperWeapons;
using Mirror;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Online superweapon salvos: the host relays every fire; clients fire the same salvo for its shots, impacts and
    /// stun effects (their health ignores damage). Registered with the superweapons module; idle offline.
    /// </summary>
    public sealed class SuperWeaponReplication : IInitializable, ILateDisposable
    {
        private readonly SuperWeaponFireService _fire;
        private readonly IEntityLocator _entities;
        private readonly IMatchSession _session;
        private readonly OnlineMatchService _online;

        public SuperWeaponReplication(SuperWeaponFireService fire, IEntityLocator entities, IMatchSession session,
            OnlineMatchService online)
        {
            _fire = fire;
            _entities = entities;
            _session = session;
            _online = online;
        }

        public void Initialize()
        {
            if (!_session.IsOnline) return;
            if (_session.IsAuthority) _fire.Fired += OnFired;
            else NetworkClient.RegisterHandler<SuperWeaponFiredMessage>(OnHostFired);
        }

        public void LateDispose()
        {
            if (!_session.IsOnline) return;
            if (_session.IsAuthority) _fire.Fired -= OnFired;
            else NetworkClient.UnregisterHandler<SuperWeaponFiredMessage>();
        }

        private void OnFired(PlayerId issuer, SuperWeaponType type, IEntity target)
        {
            if (_online.HasBegun) HostBroadcast.ToClients(_online, new SuperWeaponFiredMessage { Issuer = issuer, Type = type, TargetId = target.Id });
        }

        private void OnHostFired(SuperWeaponFiredMessage message)
        {
            if (_entities.TryGetEntity(message.TargetId, out IEntity target)) _fire.Fire(message.Issuer, message.Type, target);
        }
    }
}
