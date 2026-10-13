using EmpireAtWar.Components.Combat;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Services.Audio
{
    public sealed class ShipSfxHandle
    {
        private readonly IHealthModelObserver _health;
        private readonly CombatModifiers _modifiers;

        public long Id { get; }
        public PlayerId Owner { get; }
        public bool IsDestroyed => _health.IsDestroyed;
        public bool IsCloaked => _modifiers.IsCloaked;

        public ShipSfxHandle(long entityId, PlayerId owner, IHealthModelObserver health, CombatModifiers modifiers)
        {
            Id = entityId;
            Owner = owner;
            _health = health;
            _modifiers = modifiers;
        }
    }
}
