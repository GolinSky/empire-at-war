using EmpireAtWar.Entities.Game;
using global::VContainer;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>Registers one optional battle feature. A disabled feature registers its no-op stand-ins instead,
    /// so consumers always receive a single dependency.</summary>
    public interface IBattleModule
    {
        BattleFeatures Feature { get; }
        void ConfigureBattle(IContainerBuilder builder);
        void ConfigureDisabled(IContainerBuilder builder);
        /// <summary>Simulation registrations for every player, local or remote.</summary>
        /// <param name="runsAi">True only where this machine runs the slot's AI. A client mirrors AI slots as
        /// remote players, so <c>player.Slot.IsAi</c> alone must not add AI services.</param>
        void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot, bool runsAi);
        /// <summary>UI, input and announcements for the human at this machine only.</summary>
        void ConfigureLocalPlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot);
    }
}
