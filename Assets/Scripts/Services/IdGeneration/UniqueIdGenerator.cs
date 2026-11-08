using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.IdGeneration
{
    public interface IUniqueIdGenerator: IService
    {
        long GenerateUniqueId();
    }

    /// <summary>
    /// Battle-scope entity ids: sequential and allocated only by the authority, so a client can spawn the
    /// host's entities under the host's ids. Every registered entity (including restored ones) advances the
    /// counter past its id, so new ids never collide with loaded ones.
    /// </summary>
    public class UniqueIdGenerator: Service, IUniqueIdGenerator, IInitializable, ILateDisposable
    {
        private readonly IMatchSession _session;
        private readonly IEntityLocator _entities;
        private long _lastId;

        public UniqueIdGenerator(IMatchSession session, IEntityLocator entities)
        {
            _session = session;
            _entities = entities;
        }

        public void Initialize()
        {
            foreach (IEntity entity in _entities.Entities) Reserve(entity);
            _entities.EntityAdded += Reserve;
        }

        public void LateDispose() => _entities.EntityAdded -= Reserve;

        public long GenerateUniqueId()
        {
            if (!_session.IsAuthority)
                throw new InvalidOperationException("Only the authority allocates entity ids; clients spawn with the host's ids.");
            return ++_lastId;
        }

        private void Reserve(IEntity entity)
        {
            if (entity.Id > _lastId) _lastId = entity.Id;
        }
    }
}
