using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// Saves every living ship, squadron and structure and puts them back: everything is spawned first,
    /// then health, abilities and hangars are restored, and orders are issued last so targets and carriers exist.
    /// </summary>
    public sealed class UnitSaveService
    {
        private readonly IEntityLocator _units;
        private readonly BattleUnitSpawner _spawner;
        private readonly ShipAbilityService _abilities;

        public UnitSaveService(IEntityLocator units, BattleUnitSpawner spawner, ShipAbilityService abilities)
        {
            _units = units;
            _spawner = spawner;
            _abilities = abilities;
        }

        public UnitSave[] CaptureUnits()
        {
            List<UnitSave> saves = new List<UnitSave>();
            foreach (IEntity entity in _units.Entities)
            {
                // Dying squadrons stay registered through their death presentation.
                if (entity.HealthModel.IsDestroyed || !entity.TryGetFacade(out IUnitSaveFacade unit)) continue;
                UnitSave save = unit.Capture();
                if (entity.TryGetFacade(out IShipAbilityFacade abilities))
                    save.WithAbilities(abilities.Slots.Select(AbilitySlotSave.From).ToArray());
                if (entity.TryGetFacade(out IHangarSaveFacade hangar)) save.WithHangar(hangar.Capture());
                saves.Add(save);
            }

            return saves.ToArray();
        }

        public StructureSave[] CaptureStructures()
        {
            List<StructureSave> saves = new List<StructureSave>();
            foreach (IEntity entity in _units.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !entity.TryGetFacade(out IStructureSaveFacade structure)) continue;
                StructureSave save = structure.Capture();
                if (entity.TryGetFacade(out IHangarSaveFacade hangar)) save.WithHangar(hangar.Capture());
                saves.Add(save);
            }

            return saves.ToArray();
        }

        public BattleRestoreContext Restore(BattleSave save)
        {
            BattleRestoreContext context = new BattleRestoreContext(save);
            foreach (StructureSave structure in save.Structures) Spawn(structure, context);
            foreach (UnitSave unit in save.Units)
            {
                if (unit.UnitType.IsShip)
                    context.Ships.Add(unit.Id, _spawner.SpawnRestoredShip(unit.Owner, unit.UnitType.ShipType,
                        unit.Position, unit.Rotation, unit.Id));
                else
                    context.Squadrons.Add(unit.Id, _spawner.SpawnRestoredSquadron(unit.Owner, unit.UnitType.SquadronType,
                        unit.Position, unit.Rotation, unit.Id));
            }

            foreach (StructureSave structure in save.Structures)
            {
                IEntity entity = _units.GetEntity(structure.Id);
                entity.GetFacade<IStructureSaveFacade>().RestoreHealth(structure.Health);
                RestoreHangar(entity, structure.Hangar, context);
            }

            foreach (UnitSave unit in save.Units)
            {
                IEntity entity = _units.GetEntity(unit.Id);
                entity.GetFacade<IUnitSaveFacade>().RestoreHealth(unit.Health);
                RestoreAbilities(entity, unit.Abilities);
                RestoreHangar(entity, unit.Hangar, context);
            }

            foreach (CaptureSiteSave site in save.CaptureSites)
                if (site.State == CaptureSiteState.Operational) context.SiteFacilities.Add(site.FacilityId);
            foreach (UnitSave unit in save.Units) unit.Order.Apply(_units.GetEntity(unit.Id), _units);
            return context;
        }

        private void Spawn(StructureSave structure, BattleRestoreContext context)
        {
            switch (structure.Kind)
            {
                case StructureKind.SpaceStation:
                    _spawner.SpawnRestoredStation(structure.Owner, structure.StationFaction, structure.Position, structure.Id);
                    break;
                case StructureKind.MiningFacility:
                    context.MiningFacilities.Add(structure.Id, _spawner.SpawnRestoredMiningFacility(structure.Owner,
                        structure.MiningFacilityType, structure.Position, structure.Id));
                    break;
                case StructureKind.DefendPlatform:
                    context.DefendPlatforms.Add(structure.Id, _spawner.SpawnRestoredDefendPlatform(structure.Owner,
                        structure.DefendPlatformType, structure.Position, structure.Id));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(structure), structure.Kind, "Unknown structure kind.");
            }
        }

        private static void RestoreHangar(IEntity entity, HangarSave hangar, BattleRestoreContext context)
        {
            if (hangar == null) return;
            entity.GetFacade<IHangarSaveFacade>().Restore(hangar, context.Squadrons);
            foreach (LaunchedSquadronSave launched in hangar.Launched) context.HangarSquadrons.Add(launched.SquadronId);
        }

        private void RestoreAbilities(IEntity entity, IReadOnlyList<AbilitySlotSave> saves)
        {
            if (saves.Count == 0) return;
            IReadOnlyList<ShipAbilitySlot> slots = entity.GetFacade<IShipAbilityFacade>().Slots;
            if (slots.Count != saves.Count)
                throw new InvalidOperationException($"Saved {saves.Count} ability slots; unit {entity.Id} has {slots.Count}.");
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Id != saves[i].Id)
                    throw new InvalidOperationException($"Saved ability {saves[i].Id} does not match slot {slots[i].Id}.");
                _abilities.Restore(slots[i], saves[i].State, saves[i].TimeLeft);
            }
        }
    }
}
