using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a capture site of one kind reaches a stage (revealed, captured, building, mining).</summary>
    public sealed class CaptureSiteHintCondition : IPolledHintCondition, IHintWorldTarget
    {
        private readonly ICaptureSiteDirectory _sites;
        private readonly ILocalPlayer _player;
        private readonly CaptureSiteKind _kind;
        private readonly CaptureSiteStage _stage;
        private Vector3 _position;

        public bool IsMet { get; private set; }

        public CaptureSiteHintCondition(ICaptureSiteDirectory sites, ILocalPlayer player, CaptureSiteKind kind,
            CaptureSiteStage stage)
        {
            _sites = sites;
            _player = player;
            _kind = kind;
            _stage = stage;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != _kind || !Reached(site)) continue;
                _position = site.Center;
                IsMet = true;
                return;
            }
        }

        private bool Reached(ICaptureSite site)
        {
            switch (_stage)
            {
                case CaptureSiteStage.Revealed: return site.IsRevealed;
                case CaptureSiteStage.Captured: return _player.IsLocal(site.Owner);
                case CaptureSiteStage.Building:
                    return _player.IsLocal(site.Owner) && site.State != CaptureSiteState.Owned &&
                           site.State != CaptureSiteState.Neutral;
                default:
                    return _player.IsLocal(site.Owner) && site.State == CaptureSiteState.Operational &&
                           site.FacilityType == SiteFacilityType.Mining;
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
