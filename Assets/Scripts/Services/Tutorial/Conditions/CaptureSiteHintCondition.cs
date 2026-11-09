using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a capture site of one kind is revealed, or, with <c>requireOwned</c>, owned and running.</summary>
    public sealed class CaptureSiteHintCondition : IPolledHintCondition, IHintWorldTarget
    {
        private readonly ICaptureSiteDirectory _sites;
        private readonly ILocalPlayer _player;
        private readonly CaptureSiteKind _kind;
        private readonly bool _requireOwned;
        private Vector3 _position;

        public bool IsMet { get; private set; }

        public CaptureSiteHintCondition(ICaptureSiteDirectory sites, ILocalPlayer player, CaptureSiteKind kind,
            bool requireOwned)
        {
            _sites = sites;
            _player = player;
            _kind = kind;
            _requireOwned = requireOwned;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != _kind) continue;
                bool matches = _requireOwned
                    ? _player.IsLocal(site.Owner) && site.IsOperational
                    : site.IsRevealed;
                if (!matches) continue;
                _position = site.Center;
                IsMet = true;
                return;
            }
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = _position;
            return IsMet;
        }

        public void Dispose()
        {
        }
    }
}
