using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.Audio
{
    [LifecycleOrder(Initialize = LifecycleOrders.INTERFACE_SFX_INITIALIZE)]
    public sealed class AbilityTargetingSfx : IInitializable, ILateDisposable
    {
        private readonly ShipAbilityService _abilities;
        private readonly IInterfaceSfxService _audio;
        private readonly ILocalPlayer _localPlayer;

        public AbilityTargetingSfx(ShipAbilityService abilities, IInterfaceSfxService audio, ILocalPlayer localPlayer)
        {
            _abilities = abilities;
            _audio = audio;
            _localPlayer = localPlayer;
        }

        public void Initialize()
        {
            _abilities.TargetRejected += HandleRejected;
            _abilities.TargetingCanceled += HandleTargetingCanceled;
        }

        private void HandleRejected(IReadOnlyList<IEntity> casters)
        {
            if (HasLocalCaster(casters)) _audio.PlayRejected();
        }

        private void HandleTargetingCanceled(IReadOnlyList<IEntity> casters)
        {
            if (HasLocalCaster(casters)) _audio.PlayAbilityTargetingCanceled();
        }

        private bool HasLocalCaster(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity caster in casters)
                if (caster.Owner == _localPlayer.Id) return true;
            return false;
        }

        public void LateDispose()
        {
            _abilities.TargetRejected -= HandleRejected;
            _abilities.TargetingCanceled -= HandleTargetingCanceled;
        }
    }
}
