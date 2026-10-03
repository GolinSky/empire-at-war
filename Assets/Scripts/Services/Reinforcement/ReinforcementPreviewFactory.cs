using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Views.Reinforcement;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.Reinforcement
{
    public sealed class ReinforcementPreviewFactory
    {
        private readonly IStationFacingService _stationFacingService;

        private readonly PlayerSlot _owner;

        public ReinforcementPreviewFactory(IStationFacingService stationFacingService, PlayerSlot owner)
        {
            _stationFacingService = stationFacingService;
            _owner = owner;
        }

        public UnitSpawnView Create(UnitSpawnView prefab)
        {
            UnitSpawnView preview = Object.Instantiate(prefab);
            preview.UpdatePosition(preview.Position);
            preview.SetRotation(_stationFacingService.GetRotation(_owner.Id));
            return preview;
        }
    }
}
