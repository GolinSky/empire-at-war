using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>A player-scope save source that registers itself with the battle's <see cref="PlayerSaveRegistry"/>.</summary>
    public abstract class PlayerSaveSource : IPlayerSaveSource, IInitializable, ILateDisposable
    {
        private readonly PlayerSaveRegistry _registry;

        protected PlayerId Owner { get; }

        protected PlayerSaveSource(PlayerSaveRegistry registry, PlayerSlot owner)
        {
            _registry = registry;
            Owner = owner.Id;
        }

        public void Initialize() => _registry.Register(Owner, this);

        public void LateDispose() => _registry.Unregister(Owner, this);

        public abstract void Capture(PlayerSave save);

        public abstract void RestoreBeforeUnits(PlayerSave save);

        public virtual void RestoreAfterUnits(PlayerSave save, BattleRestoreContext context)
        {
        }
    }
}
