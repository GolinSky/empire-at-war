using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    public static class EditorToolCatalog
    {
        public const string MENU_ROOT = "Tools/Empire At War/";
        public const string HUB_MENU_PATH = MENU_ROOT + "Editor Hub";

        public static IReadOnlyList<EditorToolEntry> Discover()
        {
            var menus = TypeCache.GetMethodsWithAttribute<MenuItem>()
                .SelectMany(method => method.GetCustomAttributes<MenuItem>()
                    .Select(item => new { Method = method, Item = item }))
                .Where(entry => entry.Item.menuItem.StartsWith(MENU_ROOT, System.StringComparison.Ordinal)
                    && entry.Item.menuItem != HUB_MENU_PATH).ToArray();
            var validators = menus.Where(entry => entry.Item.validate)
                .ToDictionary(entry => entry.Item.menuItem, entry => entry.Method);
            return menus.Where(entry => !entry.Item.validate)
                .Select(entry => new EditorToolEntry(entry.Item.menuItem, entry.Method,
                    validators.TryGetValue(entry.Item.menuItem, out MethodInfo validator) ? validator : null))
                .OrderBy(entry => entry.IsLegacy).ThenBy(entry => entry.Category)
                .ThenBy(entry => entry.Group).ThenBy(entry => entry.Title).ToArray();
        }
    }
}
