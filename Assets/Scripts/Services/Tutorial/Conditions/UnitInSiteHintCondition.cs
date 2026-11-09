using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a local unit is inside the ring of a site of one kind the player does not own yet, or owns one.</summary>
    public sealed class UnitInSiteHintCondition : IPolledHintCondition
    {
        private const float CHECK_SECONDS = 0.25f;

        private readonly ICaptureSiteDirectory _sites;
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly CaptureSiteKind _kind;
        private float _sinceCheck = CHECK_SECONDS;

        public bool IsMet { get; private set; }

        public UnitInSiteHintCondition(ICaptureSiteDirectory sites, IEntityLocator entities, ILocalPlayer player,
            CaptureSiteKind kind)
        {
            _sites = sites;
            _entities = entities;
            _player = player;
            _kind = kind;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            _sinceCheck += deltaTime;
            if (_sinceCheck < CHECK_SECONDS) return;
            _sinceCheck = 0f;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != _kind) continue;
                if (_player.IsLocal(site.Owner) || HasLocalUnitInside(site))
                {
                    IsMet = true;
                    return;
                }
            }
        }

        private bool HasLocalUnitInside(ICaptureSite site)
        {
            foreach (IEntity entity in _entities.Entities)
                if (_player.IsLocal(entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IMoveFacade _) &&
                    site.Contains(entity.GetFacade<IEntityTransformFacade>().Transform.position))
                    return true;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
