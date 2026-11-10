using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Progress on sites of the step's target kind (any mining, central, edge). Capture and building count only what
    /// happens after the condition starts, so a lesson can repeat for another site.
    /// </summary>
    public sealed class TargetSiteHintCondition : IPolledHintCondition
    {
        private const float CHECK_SECONDS = 0.25f;

        public enum Stage
        {
            UnitInside,
            Captured,
            Building
        }

        private readonly ICaptureSiteDirectory _sites;
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly HintTargets _targets;
        private readonly HintTargetId _target;
        private readonly Stage _stage;
        private readonly int _startCount;
        private float _sinceCheck = CHECK_SECONDS;

        public bool IsMet { get; private set; }

        public TargetSiteHintCondition(ICaptureSiteDirectory sites, IEntityLocator entities, ILocalPlayer player,
            HintTargets targets, HintTargetId target, Stage stage)
        {
            _sites = sites;
            _entities = entities;
            _player = player;
            _targets = targets;
            _target = target;
            _stage = stage;
            _startCount = CountReached();
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            _sinceCheck += deltaTime;
            if (_sinceCheck < CHECK_SECONDS) return;
            _sinceCheck = 0f;
            IsMet = _stage == Stage.UnitInside ? HasUnitInFreeSite() : CountReached() > _startCount;
        }

        private int CountReached()
        {
            int count = 0;
            foreach (ICaptureSite site in _sites.Sites)
                if (_targets.IsSiteOf(_target, site) && _player.IsLocal(site.Owner) &&
                    (_stage != Stage.Building || site.State != CaptureSiteState.Owned && site.State != CaptureSiteState.Neutral))
                    count++;
            return count;
        }

        private bool HasUnitInFreeSite()
        {
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (!_targets.IsSiteOf(_target, site) || _player.IsLocal(site.Owner)) continue;
                foreach (IEntity entity in _entities.Entities)
                    if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                        entity.TryGetFacade(out IMoveFacade _) &&
                        site.Contains(entity.GetFacade<IEntityTransformFacade>().Transform.position))
                        return true;
            }
            return false;
        }

        public void Dispose()
        {
        }
    }
}
