using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.LightWeightFramework.PopupCommands
{
    public interface ISkirmishSetupCommand:ICommand
    {
        void StartGame(FactionType factionType);
    }

    public class SkirmishSetUpCommand : ISkirmishSetupCommand
    {
        private readonly ISceneService sceneService;
        private readonly SkirmishGameData skirmishGameData;

        public SkirmishSetUpCommand(ISceneService sceneService, SkirmishGameData skirmishGameData)
        {
            this.sceneService = sceneService;
            this.skirmishGameData = skirmishGameData;
        }
        
        public void StartGame(FactionType factionType)
        {
            Debug.Log($"factionType:{factionType}");
            skirmishGameData.PlayerFactionType = factionType;
            sceneService.LoadScene(SceneType.Skirmish);
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            command = default;
            return false;
        }
    }
}