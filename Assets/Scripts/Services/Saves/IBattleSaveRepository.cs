using System.Collections.Generic;
using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Services.Saves
{
    public interface IBattleSaveRepository
    {
        /// <summary>Writes to the slot and mode named in the save's header, replacing that file.
        /// Throws <see cref="System.IO.IOException"/> or <see cref="System.UnauthorizedAccessException"/> when the file cannot be written.</summary>
        void Save(BattleSave save);

        /// <summary>Throws <see cref="System.IO.FileNotFoundException"/> for an empty slot and
        /// <see cref="System.IO.InvalidDataException"/> for an unreadable, foreign-mode or older-version save.</summary>
        BattleSave Load(GameMode mode, BattleSaveSlot slot);

        /// <summary>Removes the slot file. File access errors propagate to the caller.</summary>
        void Delete(GameMode mode, BattleSaveSlot slot);

        /// <summary>Headers of the readable saves of one mode, newest first; older versions are listed but not loadable.</summary>
        IReadOnlyList<BattleSaveHeader> List(GameMode mode);
    }
}
