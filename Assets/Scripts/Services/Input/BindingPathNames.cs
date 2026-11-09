using UnityEngine.InputSystem;

namespace EmpireAtWar.Services.Input
{
    /// <summary>
    /// Key names from the bound control path, not from the connected keyboard, so a non-Latin OS keyboard layout
    /// still reads "W" instead of "Ц". Unity's <c>GetBindingDisplayString</c> asks the device and is layout-dependent.
    /// </summary>
    public static class BindingPathNames
    {
        public static string Readable(string path) => InputControlPath.ToHumanReadableString(path,
            InputControlPath.HumanReadableStringOptions.OmitDevice |
            InputControlPath.HumanReadableStringOptions.UseShortNames);
    }
}
