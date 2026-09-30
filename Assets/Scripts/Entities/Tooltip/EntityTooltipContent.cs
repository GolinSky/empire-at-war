using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class EntityTooltipContent
    {
        public static TooltipContent Build(IEntity entity, FactionsData factions)
        {
            IHealthModelObserver health = entity.HealthModel;
            var stats = new List<TooltipStat>();
            if (health is IHealthTooltipObserver capacity)
            {
                stats.Add(new TooltipStat("Hull", health.Hull, capacity.MaxHull));
                if (capacity.MaxShields > 0f) stats.Add(new TooltipStat("Shields", health.Shields, capacity.MaxShields));
                if (capacity.ShieldRegeneration > 0f)
                {
                    stats.Add(new TooltipStat("Shield regeneration per tick", capacity.ShieldRegeneration));
                    stats.Add(new TooltipStat("Regeneration interval (s)", capacity.ShieldRegenerationInterval));
                }
            }
            else stats.Add(new TooltipStat("Hull", health.Hull));
            if (entity.Model is ISquadronModelObserver)
                stats.Add(new TooltipStat("Surviving fighters", health.HardPointModels.Count(member => !member.IsDestroyed),
                    health.HardPointModels.Length));
            string order = entity.TryGetFacade(out IUnitOrderObserverFacade orders)
                ? $" · Order: {orders.CurrentOrder}" : "";
            string status = $"Owner: {entity.Owner} · {health.ShipClass}{order}";
            if (entity.TryGetFacade(out ICombatModifiersFacade combat))
            {
                var modifiers = combat.Modifiers;
                if (modifiers.DamageMultiplier != 1f) stats.Add(new TooltipStat("Damage modifier", modifiers.DamageMultiplier, format: "0.##'×'"));
                if (modifiers.SpeedMultiplier != 1f) stats.Add(new TooltipStat("Speed modifier", modifiers.SpeedMultiplier, format: "0.##'×'"));
                if (modifiers.DamageTakenMultiplier != 1f) stats.Add(new TooltipStat("Damage taken", modifiers.DamageTakenMultiplier, format: "0.##'×'"));
            }
            if (entity.Model is IShipModelObserver ship)
                return UnitTooltipContent.Build(factions.GetShipFactionData(ship.ShipType), stats, status: status);
            if (entity.Model is ISquadronModelObserver squadron)
                return UnitTooltipContent.Build(factions.GetSquadronFactionData(squadron.SquadronType), stats, status: status);
            if (entity.Model is EmpireAtWar.Entities.SpaceStation.ISpaceStationModelObserver)
                return new TooltipContent("Space station", "Produces units, research and upgrades. Select to open the station roster.", stats: stats, status: status);
            if (entity.Model is EmpireAtWar.Entities.MiningFacility.MiningFacilityData mine)
            {
                stats.Add(new TooltipStat("Base income per payment", mine.Income));
                return new TooltipContent("Mining facility", "Provides recurring credits to its owner.", stats: stats, status: status);
            }
            if (entity.Model is EmpireAtWar.Entities.DefendPlatform.IDefendPlatformModelObserver)
                return new TooltipContent("Defensive platform", "Defends the surrounding area.", stats: stats, status: status);
            return new TooltipContent(health.ShipClass.ToString(), stats: stats, status: status);
        }
    }
}
