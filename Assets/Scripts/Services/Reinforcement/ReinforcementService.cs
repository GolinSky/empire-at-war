using EmpireAtWar.Entities.Reinforcement;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Views.SpawnArea;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService
    {
        void TrySpawnReinforcement(UnitRequest request);
    }

    /// <summary>Runs the drag-to-place flow: input lock, preview, validation and release.
    /// What may be placed where is decided by the request's <see cref="IReinforcementPlacement"/>.
    /// Holding deploy-all at drag start places every reserve unit of that kind that fits, in a grid around the cursor.</summary>
    public class ReinforcementService : Service, IReinforcementService, ILateTickable, IInitializable,
        ILateDisposable, IReinforcementPool, IObserver<BattleResult>, IUiCancelHandler
    {
        // Gap between batch slots, relative to the preview's planar radius.
        private const float SLOT_SPACING_FACTOR = 2.2f;
        private readonly IInputLock _inputLock;
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _uiHitTest;
        private readonly IUiCancelRouter _cancelRouter;
        private readonly ICameraService _cameraService;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly ISpawnAreaOverlay _spawnAreaOverlay;
        private readonly IReinforcementDeployInput _deployInput;
        private readonly IStructureSpawnClearance _structureClearance;
        private IDisposable _placementLock;
        private IReinforcementPlacement _placement;

        private readonly ReinforcementModel _model;
        private readonly ReinforcementPlacementFactory _placementFactory;
        private readonly PlayerSlot _owner;
        private readonly IPlayerRegistry _playerRegistry;
        private readonly List<UnitSpawnView> _previews = new List<UnitSpawnView>();
        private UnitRequest _currentRequest;
        private float _slotSpacing;

        private bool _hasBattleEnded;
        private readonly IHazardService _hazards;
        private readonly TooltipRequests _tooltips;

        public event Action<UnitRequest> ReinforcementDeployed;
        public event Action CapacityRejected;

        public ReinforcementService(
            IInputLock inputLock,
            IPointerInput pointerInput,
            ICameraService cameraService,
            INotifier<BattleResult> battleVictoryNotifier,
            ISpawnAreaOverlay spawnAreaOverlay,
            ReinforcementModel model,
            ReinforcementPlacementFactory placementFactory,
            PlayerSlot owner,
            IPlayerRegistry playerRegistry,
            IUiHitTest uiHitTest,
            IUiCancelRouter cancelRouter,
            IHazardService hazards,
            ITooltipService tooltipService,
            IReinforcementDeployInput deployInput,
            IStructureSpawnClearance structureClearance)
        {
            _playerRegistry = playerRegistry;
            _owner = owner;
            _model = model;
            _placementFactory = placementFactory;
            _inputLock = inputLock;
            _pointerInput = pointerInput;
            _cameraService = cameraService;
            _battleVictoryNotifier = battleVictoryNotifier;
            _spawnAreaOverlay = spawnAreaOverlay;
            _uiHitTest = uiHitTest;
            _cancelRouter = cancelRouter;
            _hazards = hazards;
            _tooltips = new TooltipRequests(tooltipService);
            _deployInput = deployInput;
            _structureClearance = structureClearance;
        }

        public void Initialize()
        {
            _playerRegistry.RegisterReinforcementReserve(_owner.Id, _model);
            _pointerInput.PrimaryReleased += Interrupt;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            CancelPlacement();
            _playerRegistry.UnregisterReinforcementReserve(_owner.Id);
            _pointerInput.PrimaryReleased -= Interrupt;
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
            CancelPlacement();
            // The battle is over: gameplay input stays locked for the rest of the scene.
            _inputLock.Acquire();
        }

        // Runs after the camera moves so the preview stays under the cursor while the view pans.
        public void LateTick()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            Vector3 position = _cameraService.GetWorldPoint(_pointerInput.Position, _previews[0].Position);
            position.y = 0;
            bool isOverUi = _uiHitTest.IsOverUi(_pointerInput.Position);
            for (int i = 0; i < _previews.Count; i++)
            {
                Vector3 slot = GetSlotPosition(position, i);
                _previews[i].UpdatePosition(slot);
                _previews[i].SetPlacementValidity(!isOverUi && _placement.IsPositionValid(slot));
            }
            if (_hazards.TryGetHazardAt(position, out var hazard) && hazard.BlocksReinforcements)
                _tooltips.Show(this, hazard, new TooltipAnchor(TooltipAnchorKind.Cursor,
                    _pointerInput.Position.x, _pointerInput.Position.y), () => _model.IsTrySpawning,
                    () => new TooltipContent(title: hazard.DisplayName,
                        description: "Reinforcements cannot arrive inside this hazard."));
            else
                _tooltips.Hide(this);
        }

        public void Add(UnitRequest request)
        {
            switch (request)
            {
                case ShipUnitRequest shipUnitRequest:
                    _model.UpdateShipData(shipUnitRequest);
                    _model.AddReinforcement(shipUnitRequest);
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    _model.UpdateSquadronData(squadronUnitRequest);
                    _model.AddReinforcement(squadronUnitRequest);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    _model.AddReinforcement(miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    _model.AddReinforcement(defendPlatformUnitRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        public void TrySpawnReinforcement(UnitRequest request)
        {
            // Units already in the pool stay deployable after the station falls.
            if (_hasBattleEnded || _inputLock.IsLocked)
            {
                _model.InvokeSpawnShipEvent(0);
                return;
            }

            if (!_placementFactory.TryCreate(request, out IReinforcementPlacement placement))
            {
                CapacityRejected?.Invoke();
                _model.InvokeSpawnShipEvent(0);
                return;
            }

            // A placement that is still running would otherwise keep its input lock forever.
            CancelPlacement();
            _currentRequest = request;
            _placement = placement;
            // Battle input only: the camera stays movable so the player can pan to a drop point.
            _placementLock = _inputLock.AcquireBattle();
            _model.IsTrySpawning = true;
            int count = _deployInput.IsDeployAllHeld
                ? _model.GetDeployableCount(request)
                : 1;
            for (int i = 0; i < count; i++)
                _previews.Add(placement.CreatePreview());
            _slotSpacing = _previews[0].PlanarRadius * SLOT_SPACING_FACTOR;
            bool placingStructure = !ReinforcementModel.UsesCapacity(request);
            if (placingStructure)
                _slotSpacing = Mathf.Max(_slotSpacing, _structureClearance.Radius * SLOT_SPACING_FACTOR);
            _spawnAreaOverlay.Show(_owner.Id, placingStructure);
            _cancelRouter.Focus(this);
        }

        public bool TryCancel()
        {
            if (!_model.IsTrySpawning) return false;
            CancelPlacement();
            return true;
        }

        private void CancelPlacement()
        {
            _tooltips.HideAll();
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            _cancelRouter.Unfocus(this);
            DestroyPreviews();
            _spawnAreaOverlay.Hide();
            _placementLock.Dispose();
            _model.InvokeSpawnShipEvent(0);
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            if (_inputLock.IsLocked || _uiHitTest.IsOverUi(screenPosition))
            {
                CancelPlacement();
                return;
            }

            _model.IsTrySpawning = false;
            _cancelRouter.Unfocus(this);
            Vector3 dropPoint = _cameraService.GetWorldPoint(screenPosition, _previews[0].Position);
            bool usesCapacity = ReinforcementModel.UsesCapacity(_currentRequest);
            bool isCapacityBlocked = false;
            int deployedCount = 0;
            // Blocked slots are skipped; their units stay in the reserve.
            for (int i = 0; i < _previews.Count; i++)
            {
                Vector3 slot = GetSlotPosition(dropPoint, i);
                if (!_previews[i].CanSpawn || !_placement.IsPositionValid(slot)) continue;
                if (usesCapacity && !_model.HasRoomFor(_currentRequest))
                {
                    isCapacityBlocked = true;
                    break;
                }

                _placement.Spawn(slot);
                _model.ConsumeReinforcement(_currentRequest);
                deployedCount++;
                ReinforcementDeployed?.Invoke(_currentRequest);
            }

            if (isCapacityBlocked || deployedCount == 0 && usesCapacity && !_model.HasRoomFor(_currentRequest))
            {
                CapacityRejected?.Invoke();
            }

            DestroyPreviews();
            _spawnAreaOverlay.Hide();
            _placementLock.Dispose();
            _model.InvokeSpawnShipEvent(deployedCount);
        }

        private Vector3 GetSlotPosition(Vector3 dropPoint, int index)
        {
            FormationPoint slot = FormationModel.CalculateGridDestination(index, _previews.Count,
                new FormationPoint(dropPoint.x, dropPoint.z), _slotSpacing);
            return new Vector3(slot.X, dropPoint.y, slot.Z);
        }

        private void DestroyPreviews()
        {
            foreach (UnitSpawnView preview in _previews)
                preview.Destroy();
            _previews.Clear();
        }
    }
}
