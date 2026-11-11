using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Saves
{
    internal static class HangarStockMigration
    {
        public static bool TryMigrate(UnitSave unit, IReadOnlyList<HangarBay> bays, out HangarSave migrated)
        {
            int[] oldStock = GetVersion4Stock(unit.UnitType.ShipType);
            if (oldStock.Length == 0)
            {
                migrated = null;
                return false;
            }

            HangarSave hangar = unit.Hangar;
            if (hangar == null || hangar.Reserve == null || hangar.Reserve.Length != oldStock.Length)
                throw new InvalidDataException($"Expected {oldStock.Length} version 4 hangar bays for entity {unit.Id}.");
            if (bays.Count < oldStock.Length)
                throw new InvalidOperationException($"Current hangar data for {unit.UnitType.ShipType} removed version 4 bays.");

            foreach (LaunchedSquadronSave launched in hangar.Launched)
                if (launched.Bay < 0 || launched.Bay >= oldStock.Length)
                    throw new InvalidDataException($"Invalid version 4 hangar bay {launched.Bay} on entity {unit.Id}.");

            int[] remaining = new int[bays.Count];
            for (int bay = 0; bay < oldStock.Length; bay++)
            {
                int savedRemaining = hangar.Reserve[bay];
                if (savedRemaining < 0)
                    throw new InvalidDataException($"Negative version 4 hangar reserve at bay {bay} on entity {unit.Id}.");
                int spent = Math.Max(0, oldStock[bay] - savedRemaining);
                remaining[bay] = Math.Max(0, bays[bay].Reserve - spent);
            }
            for (int bay = oldStock.Length; bay < bays.Count; bay++)
                remaining[bay] = bays[bay].Reserve;

            migrated = HangarSave.Create(remaining, hangar.Cooldown, hangar.Launched);
            return true;
        }

        // Starting reserves before the rebalance, in their original serialized bay order.
        private static int[] GetVersion4Stock(ShipType shipType) => shipType switch
        {
            ShipType.Wavecrest => new[] { 3 },
            ShipType.Bulwark => new[] { 4, 2 },
            ShipType.Gladiator => new[] { 10, 10, 10, 10, 10, 10 },
            ShipType.Executor or ShipType.BothanAssaultCruiser or ShipType.CorellianBattlecruiser or
                ShipType.Dauntless or ShipType.HomeOne or ShipType.MC75Profundity or ShipType.MC80Independence or
                ShipType.Nebula or ShipType.QuasarFire or ShipType.Imperator or ShipType.Mandator or ShipType.Resolute or
                ShipType.Rothana => new[] { 20, 20 },
            ShipType.Invincible or ShipType.Lucrehulk or ShipType.Providence => new[] { 20, 20, 20 },
            ShipType.Acclamator or ShipType.AcclamatorAssault or ShipType.Captor or ShipType.Endurance or
                ShipType.ImperialVenator or ShipType.Liberator or ShipType.Venator or ShipType.VenatorII => new[] { 40, 40, 40 },
            ShipType.ISDI or ShipType.ISDII => new[] { 80, 80, 80 },
            _ => Array.Empty<int>()
        };
    }
}
