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
        void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot);
    }
}
