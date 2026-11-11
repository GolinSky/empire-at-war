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
        public const int CURRENT_VERSION = 5;
        internal const int PRE_HANGAR_REBALANCE_VERSION = 4;

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
        public bool IsCompatible => version is CURRENT_VERSION or PRE_HANGAR_REBALANCE_VERSION;

        internal void FinishHangarMigration() => version = CURRENT_VERSION;

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
