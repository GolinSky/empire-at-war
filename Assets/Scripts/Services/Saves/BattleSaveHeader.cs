using System;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>What a load list shows about a save without rebuilding the battle.</summary>
    [Serializable]
    public sealed class BattleSaveHeader
    {
        /// <summary>Raise when the save layout changes; older saves are refused, not migrated.</summary>
        public const int CURRENT_VERSION = 2;

        [SerializeField] private int version;
        [SerializeField] private GameMode mode;
        // The slot kind follows from the name: autosave, quicksave, or anything else.
        [SerializeField] private string slotName;
        [SerializeField] private long savedAtUtcTicks;
        [SerializeField] private float battleTime;
        [SerializeField] private GameSpeed gameSpeed;

        public int Version => version;
        public GameMode Mode => mode;
        public BattleSaveSlot Slot => BattleSaveSlot.FromName(slotName);
        public DateTime SavedAtUtc => new DateTime(savedAtUtcTicks, DateTimeKind.Utc);
        /// <summary>Battle seconds elapsed while the battle was running.</summary>
        public float BattleTime => battleTime;
        public GameSpeed GameSpeed => gameSpeed;
        public bool IsCompatible => version == CURRENT_VERSION;

        public static BattleSaveHeader Create(GameMode mode, BattleSaveSlot slot, DateTime savedAtUtc,
            float battleTime, GameSpeed gameSpeed) => new BattleSaveHeader
        {
            version = CURRENT_VERSION,
            mode = mode,
            slotName = slot.Name,
            savedAtUtcTicks = savedAtUtc.Ticks,
            battleTime = battleTime,
            gameSpeed = gameSpeed
        };
    }
}
