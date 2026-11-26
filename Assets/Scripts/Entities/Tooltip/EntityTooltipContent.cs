using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class EntityTooltipContent
    {
        public static TooltipContent Build(IEntity entity, string ownerName, string hint = null)
        {
            IHealthModelObserver health = entity.HealthModel;
            var stats = new List<TooltipStat>();
            string order = entity.TryGetFacade(out IUnitOrderObserverFacade orders)
                ? $" · Order: {orders.CurrentOrder}" : "";
            string status = $"Owner: {ownerName} · {health.ShipClass}{order}";
            if (entity.TryGetFacade(out ICombatModifiersFacade combat))
            {
                var modifiers = combat.Modifiers;
                if (modifiers.IsDamageDealtModified()) stats.Add(new TooltipStat(label: "Damage modifier", current: modifiers.DamageMultiplier, format: "0.##'×'"));
                if (modifiers.IsFireDelayModified()) stats.Add(new TooltipStat(label: "Reload time", current: modifiers.FireDelayMultiplier, format: "0.##'×'"));
                if (modifiers.IsSpeedModified()) stats.Add(new TooltipStat(label: "Speed modifier", current: modifiers.SpeedMultiplier, format: "0.##'×'"));
                if (modifiers.IsDamageTakenModified()) stats.Add(new TooltipStat(label: "Damage taken", current: modifiers.DamageTakenMultiplier, format: "0.##'×'"));
            }

            return entity.GetFacade<IEntityTooltipFacade>().Build(stats, hint == null ? status : status + "\n" + hint);
        }
    }
}
