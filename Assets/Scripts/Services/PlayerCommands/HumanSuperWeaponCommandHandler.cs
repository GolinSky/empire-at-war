using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SuperWeapons;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Fires one human's charged super weapon at a valid hostile target.</summary>
    public sealed class HumanSuperWeaponCommandHandler : ISuperWeaponCommandHandler, IInitializable, ILateDisposable
    {
        private readonly PlayerSlot _owner;
        private readonly SuperWeaponModel _model;
        private readonly ISuperWeaponFireService _fireService;
        private readonly PlayerCommandHandlers _handlers;

        public HumanSuperWeaponCommandHandler(PlayerSlot owner, SuperWeaponModel model,
            ISuperWeaponFireService fireService, PlayerCommandHandlers handlers)
        {
            _owner = owner;
            _model = model;
            _fireService = fireService;
            _handlers = handlers;
        }

        public void Initialize() => _handlers.RegisterSuperWeapons(_owner.Id, this);

        public void LateDispose() => _handlers.UnregisterSuperWeapons(_owner.Id);

        public void Fire(SuperWeaponType type, IEntity target)
        {
            if (!_model.CanFire(type) || !_fireService.CanTarget(_owner.Id, target)) return;
            _model.Consume(type);
            _fireService.Fire(type, target);
        }
    }
}
