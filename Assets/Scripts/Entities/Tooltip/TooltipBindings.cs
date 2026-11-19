using System.Linq;
using EmpireAtWar.Services.Input;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class TooltipBindings
    {
        public static string Get(IInputBindings bindings, string map, string action)
        {
            string display = string.Join(" / ", bindings.RebindableSlots
                .Where(slot => slot.Action.actionMap.name == map && slot.Action.name == action)
                .Select(bindings.GetBindingDisplayString).Distinct());
            return display.Length > 0 ? display : bindings.GetActionBindingDisplayString(map, action);
        }
    }
}
