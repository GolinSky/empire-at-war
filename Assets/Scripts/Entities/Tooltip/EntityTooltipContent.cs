using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class EntityTooltipContent
    {
        public static TooltipContent Build(IEntity entity)
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
            string order = entity.TryGetFacade(out IUnitOrderObserverFacade orders)
                ? $" · Order: {orders.CurrentOrder}" : "";
            string status = $"Owner: {entity.Owner} · {health.ShipClass}{order}";
            if (entity.TryGetFacade(out ICombatModifiersFacade combat))
            {
                var modifiers = combat.Modifiers;
                if (modifiers.IsDamageDealtModified()) stats.Add(new TooltipStat("Damage modifier", modifiers.DamageMultiplier, format: "0.##'×'"));
                if (modifiers.IsSpeedModified()) stats.Add(new TooltipStat("Speed modifier", modifiers.SpeedMultiplier, format: "0.##'×'"));
                if (modifiers.IsDamageTakenModified()) stats.Add(new TooltipStat("Damage taken", modifiers.DamageTakenMultiplier, format: "0.##'×'"));
            }

            return entity.GetFacade<IEntityTooltipFacade>().Build(stats, status);
        }
    }
}
