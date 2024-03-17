using EmpireAtWar.Models.MiningFacility;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.MiningFacility
{
    public interface IMiningFacilityCommand: ICommand
    {
        
    }
    public class MiningFacilityController : Controller<MiningFacilityModel>, IMiningFacilityCommand
    {
        private readonly Vector3 spawnPosition;

        public MiningFacilityController(MiningFacilityModel model, Vector3 startPosition) : base(model)
        {
            this.spawnPosition = startPosition;
        }
        
    }
}