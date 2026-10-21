using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Services.SuperWeapons;

namespace EmpireAtWar.Services.Saves
{
    public sealed class SuperWeaponCombatSaveSource : IBattleSaveSource
    {
        private readonly SuperWeaponFireService _fire;
        private readonly IEntityLocator _entities;
        private readonly SuperWeaponData _data;

        public SuperWeaponCombatSaveSource(SuperWeaponFireService fire, IEntityLocator entities, SuperWeaponData data)
        {
            _fire = fire;
            _entities = entities;
            _data = data;
        }

        public void Capture(BattleSave save)
        {
            // Destroyed targets are omitted from the entity snapshot, and cannot receive another hit.
            save.SuperWeaponSalvos = _fire.Salvos.Where(salvo => !salvo.Target.HealthModel.IsDestroyed)
                .Select(SuperWeaponSalvoSave.From).ToArray();
            save.SuperWeaponStuns = _fire.Stuns.Where(stun => !stun.Target.HealthModel.IsDestroyed)
                .Select(SuperWeaponStunSave.From).ToArray();
        }

        public void RestoreBeforeUnits(BattleSave save) { }

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
            // Older saves predate combat-effect snapshots.
            if (context.Save.SuperWeaponSalvos != null)
                foreach (SuperWeaponSalvoSave salvo in context.Save.SuperWeaponSalvos)
                    _fire.RestoreSalvo(salvo.Restore(_entities, _data));
            if (context.Save.SuperWeaponStuns != null)
                foreach (SuperWeaponStunSave stun in context.Save.SuperWeaponStuns)
                    _fire.RestoreStun(stun.Restore(_entities));
        }
    }
}
