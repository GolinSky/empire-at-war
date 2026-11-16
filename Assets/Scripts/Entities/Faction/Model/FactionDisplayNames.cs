using System;

namespace EmpireAtWar.Models.Factions
{
    public static class FactionDisplayNames
    {
        public static string GetDisplayName(this FactionType faction) =>
            faction == FactionType.Separatist ? "Confederation" : faction.ToString();

        public static string[] GetAll() =>
            Array.ConvertAll((FactionType[])Enum.GetValues(typeof(FactionType)), GetDisplayName);
    }
}
