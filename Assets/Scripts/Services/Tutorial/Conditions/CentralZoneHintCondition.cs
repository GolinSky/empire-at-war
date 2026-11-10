using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player owns a central mining site, or (whole zone) owns and builds on all of them.</summary>
    public sealed class CentralZoneHintCondition : IPolledHintCondition
    {
        private readonly ICaptureSiteDirectory _sites;
        private readonly ILocalPlayer _player;
        private readonly HintTargets _targets;
        private readonly bool _wholeZone;

        public bool IsMet { get; private set; }

        public CentralZoneHintCondition(ICaptureSiteDirectory sites, ILocalPlayer player, HintTargets targets,
            bool wholeZone)
        {
            _sites = sites;
            _player = player;
            _targets = targets;
            _wholeZone = wholeZone;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            bool any = false;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (!_targets.IsSiteOf(HintTargetId.CentralMiningSite, site)) continue;
                bool isTaken = _player.IsLocal(site.Owner) &&
                               (!_wholeZone || site.State != CaptureSiteState.Owned && site.State != CaptureSiteState.Neutral);
                if (_wholeZone && !isTaken) return;
                any |= isTaken;
            }
            IsMet = any;
        }

        public void Dispose()
        {
        }
    }
}
