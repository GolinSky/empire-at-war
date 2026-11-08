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
        void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot);
        /// <summary>UI, input and announcements for the human at this machine only.</summary>
        void ConfigureLocalPlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot);
    }
}
