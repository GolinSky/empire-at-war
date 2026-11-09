using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the local player activates an ability by hand (not auto-power).</summary>
    public sealed class AbilityUsedHintCondition : IHintCondition
    {
        private readonly ShipAbilityService _abilities;
        private readonly ILocalPlayer _player;

        public bool IsMet { get; private set; }

        public AbilityUsedHintCondition(ShipAbilityService abilities, ILocalPlayer player)
        {
            _abilities = abilities;
            _player = player;
            _abilities.ManualAction += HandleManualAction;
        }

        private void HandleManualAction(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity caster in casters)
                if (_player.IsLocal(caster.Owner)) IsMet = true;
        }

        public void Dispose() => _abilities.ManualAction -= HandleManualAction;
    }
}
