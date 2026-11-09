using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a hostile unit is visible to the local player. Scans twice a second.</summary>
    public sealed class EnemySeenHintCondition : IPolledHintCondition, IHintWorldTarget
    {
        private const float SCAN_INTERVAL = 0.5f;

        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly IVisionService _vision;
        private float _untilScan;
        private Vector3 _position;

        public bool IsMet { get; private set; }

        public EnemySeenHintCondition(IEntityLocator entities, ILocalPlayer player, IVisionService vision)
        {
            _entities = entities;
            _player = player;
            _vision = vision;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet) return;
            _untilScan -= deltaTime;
            if (_untilScan > 0f) return;
            _untilScan = SCAN_INTERVAL;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_player.IsHostile(entity.Owner) || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IEntityTransformFacade transform)) continue;
                Vector3 position = transform.Transform.position;
                if (!_vision.IsVisible(_player.Id, position)) continue;
                _position = position;
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
