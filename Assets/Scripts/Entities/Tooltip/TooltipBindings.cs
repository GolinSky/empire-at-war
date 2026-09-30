using System.Linq;
using EmpireAtWar.Services.Input;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class TooltipBindings
    {
        public static string Get(IInputBindings bindings, string map, string action) => string.Join(" / ",
            bindings.RebindableSlots.Where(slot => slot.Action.actionMap.name == map && slot.Action.name == action)
                .Select(bindings.GetBindingDisplayString).Distinct());
    }
}
