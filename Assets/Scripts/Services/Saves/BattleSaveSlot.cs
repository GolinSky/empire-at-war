using System;
using System.IO;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>One save file inside a mode folder: <c>autosave</c>, <c>quicksave</c> or a player-chosen name.</summary>
    public sealed class BattleSaveSlot
    {
        private const string AUTOSAVE_NAME = "autosave";
        private const string QUICKSAVE_NAME = "quicksave";

        public static readonly BattleSaveSlot Autosave = new BattleSaveSlot(BattleSaveSlotKind.Autosave, AUTOSAVE_NAME);
        public static readonly BattleSaveSlot Quicksave = new BattleSaveSlot(BattleSaveSlotKind.Quicksave, QUICKSAVE_NAME);

        public BattleSaveSlotKind Kind { get; }
        /// <summary>The file name without its extension.</summary>
        public string Name { get; }

        private BattleSaveSlot(BattleSaveSlotKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        /// <summary>A player-named slot; the name becomes the file name, so path characters and reserved names are rejected.</summary>
        public static BattleSaveSlot Named(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim() != name || name.EndsWith(".") ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"'{name}' is not a valid save name.", nameof(name));
            if (IsReserved(name))
                throw new ArgumentException($"'{name}' is reserved for automatic saves.", nameof(name));
            return new BattleSaveSlot(BattleSaveSlotKind.Named, name);
        }

        /// <summary>The slot a file name without extension belongs to.</summary>
        public static BattleSaveSlot FromName(string name)
        {
            if (string.Equals(name, AUTOSAVE_NAME, StringComparison.OrdinalIgnoreCase)) return Autosave;
            if (string.Equals(name, QUICKSAVE_NAME, StringComparison.OrdinalIgnoreCase)) return Quicksave;
            return Named(name);
        }

        private static bool IsReserved(string name) =>
            string.Equals(name, AUTOSAVE_NAME, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, QUICKSAVE_NAME, StringComparison.OrdinalIgnoreCase);
    }
}
