using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Hazards;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class HazardEnteredHintCondition : IHintCondition, IHintWorldTarget
    {
        private readonly IHazardService _hazards;
        private readonly ILocalPlayer _player;
        private Vector3 _position;

        public bool IsMet { get; private set; }

        public HazardEnteredHintCondition(IHazardService hazards, ILocalPlayer player)
        {
            _hazards = hazards;
            _player = player;
            _hazards.Entered += HandleEntered;
        }

        private void HandleEntered(IEntity entity, SpaceHazardData hazard)
        {
            if (IsMet || !_player.IsLocal(entity.Owner)) return;
            if (entity.TryGetFacade(out IEntityTransformFacade transform))
                _position = transform.Transform.position;
            IsMet = true;
        }

        public bool TryGetWorldTarget(out Vector3 position)
        {
            position = _position;
            return IsMet;
        }

        public void Dispose() => _hazards.Entered -= HandleEntered;
    }
}
