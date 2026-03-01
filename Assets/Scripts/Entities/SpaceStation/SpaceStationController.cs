using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationCommand : ICommand
    {

    }
    public class SpaceStationController : Controller<SpaceStationModel>, ISpaceStationCommand, IInitializable
    {
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly PlayerType _playerType;

        public SpaceStationController(SpaceStationModel model, FogOfWarSystem fogOfWarSystem, PlayerType playerType) : base(model)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
        }

        public void Initialize()
        {
            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(Model.DefaultMoveModel.ViewTransform.Value, 180f);
            }
        }
    }
}