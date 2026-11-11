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
using EmpireAtWar.Services.PlayerCommands;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService
    {
        void TrySpawnReinforcement(UnitRequest request);
    }

    /// <summary>Runs the drag-to-place flow: input lock, preview, validation and release.
    /// What may be placed where is decided by the request's <see cref="IReinforcementPlacement"/>.
    /// Holding deploy-all at drag start places every reserve unit of that kind that fits, in a grid around the cursor.</summary>
    public class ReinforcementService : Service, IReinforcementService, IReinforcementCommandHandler, ILateTickable, IInitializable,
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
        private readonly IPlayerCommandSink _commands;
        private readonly PlayerCommandHandlers _commandHandlers;
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
        public event Action PlacementRejected;

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
            IStructureSpawnClearance structureClearance,
            IPlayerCommandSink commands,
            PlayerCommandHandlers commandHandlers)
        {
            _commands = commands;
            _commandHandlers = commandHandlers;
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
            _commandHandlers.RegisterReinforcements(_owner.Id, this);
            _pointerInput.PrimaryReleased += Interrupt;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            CancelPlacement();
            _playerRegistry.UnregisterReinforcementReserve(_owner.Id);
            _commandHandlers.UnregisterReinforcements(_owner.Id);
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
            bool canPlace = !_inputLock.IsLocked && !_uiHitTest.IsOverUi(_pointerInput.Position) &&
                            !IsPlacementCapacityBlocked();
            bool hasValidSlot = false;
            for (int i = 0; i < _previews.Count; i++)
            {
                Vector3 slot = GetSlotPosition(position, i);
                _previews[i].UpdatePosition(slot);
                _previews[i].SetPlacementValidity(canPlace && _placement.IsPositionValid(slot));
                hasValidSlot |= _previews[i].CanSpawn;
            }
            _model.CanDeployPreview = hasValidSlot;
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
            CancelPlacement();
            // Units already in the pool stay deployable after the station falls.
            if (_hasBattleEnded || _inputLock.IsLocked)
            {
                _model.InvokePlacementEnded();
                return;
            }

            int available = _model.GetDeployableCount(request);
            if (available <= 0)
            {
                if (_model.GetReserveCount(request) > 0)
                    CapacityRejected?.Invoke();
                _model.InvokePlacementEnded();
                return;
            }

            if (!_placementFactory.TryCreate(request, out IReinforcementPlacement placement))
            {
                CapacityRejected?.Invoke();
                _model.InvokePlacementEnded();
                return;
            }

            _currentRequest = request;
            _placement = placement;
            // Battle input only: the camera stays movable so the player can pan to a drop point.
            _placementLock = _inputLock.AcquireBattle();
            _model.IsTrySpawning = true;
            try
            {
                int count = _deployInput.IsDeployAllHeld ? available : 1;
                for (int i = 0; i < count; i++)
                    _previews.Add(placement.CreatePreview());
                _slotSpacing = _previews[0].PlanarRadius * SLOT_SPACING_FACTOR;
                bool placingStructure = !ReinforcementModel.UsesCapacity(request);
                if (placingStructure)
                    _slotSpacing = Mathf.Max(_slotSpacing, _structureClearance.Radius * SLOT_SPACING_FACTOR);
                _spawnAreaOverlay.Show(_owner.Id, placingStructure);
                _cancelRouter.Focus(this);
            }
            catch
            {
                CancelPlacement();
                throw;
            }
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
            _model.CanDeployPreview = false;
            _cancelRouter.Unfocus(this);
            _placementLock.Dispose();
            DestroyPreviews();
            _spawnAreaOverlay.Hide();
            _model.InvokePlacementEnded();
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            try
            {
                if (_inputLock.IsLocked || _uiHitTest.IsOverUi(screenPosition))
                    return;

                if (IsPlacementCapacityBlocked())
                {
                    CapacityRejected?.Invoke();
                    return;
                }

                Vector3 dropPoint = _cameraService.GetWorldPoint(screenPosition, _previews[0].Position);
                // Previews flag locally blocked slots; the authority re-checks every slot it receives.
                List<Vector3> slots = new List<Vector3>(_previews.Count);
                for (int i = 0; i < _previews.Count; i++)
                {
                    Vector3 slot = GetSlotPosition(dropPoint, i);
                    _previews[i].SetPlacementValidity(_placement.IsPositionValid(slot));
                    if (_previews[i].CanSpawn) slots.Add(slot);
                }

                if (slots.Count == 0)
                {
                    PlacementRejected?.Invoke();
                    return;
                }

                UnitRequestKey key = UnitRequestKey.From(_currentRequest);
                _commands.Submit(new DeployReinforcementCommand(_owner.Id, key.Kind, key.Key, slots.ToArray()));
            }
            finally
            {
                // Submission is asynchronous on clients; ending the local drag must not wait for the host.
                CancelPlacement();
            }
        }

        private bool IsPlacementCapacityBlocked() =>
            ReinforcementModel.UsesCapacity(_currentRequest) && !_model.HasRoomFor(_currentRequest);

        /// <summary>Authority side of a drop: blocked slots are skipped and their units stay in the reserve.</summary>
        public void Deploy(DeployReinforcementCommand command)
        {
            if (!TryFindReserve(command.Kind, command.Key, out UnitRequest request))
            {
                PlacementRejected?.Invoke();
                return;
            }

            if (!_placementFactory.TryCreate(request, out IReinforcementPlacement placement))
            {
                CapacityRejected?.Invoke();
                return;
            }

            bool usesCapacity = ReinforcementModel.UsesCapacity(request);
            bool isCapacityBlocked = false;
            int deployedCount = 0;
            foreach (Vector3 slot in command.Slots)
            {
                if (_model.GetReserveCount(request) == 0) break;
                if (!placement.IsPositionValid(slot)) continue;
                if (usesCapacity && !_model.HasRoomFor(request))
                {
                    isCapacityBlocked = true;
                    break;
                }

                placement.Spawn(slot);
                _model.ConsumeReinforcement(request);
                deployedCount++;
                ReinforcementDeployed?.Invoke(request);
            }

            if (isCapacityBlocked || deployedCount == 0 && usesCapacity && !_model.HasRoomFor(request))
            {
                CapacityRejected?.Invoke();
            }
            else if (deployedCount == 0)
            {
                PlacementRejected?.Invoke();
            }
        }

        private bool TryFindReserve(UnitRequestKind kind, int key, out UnitRequest request)
        {
            foreach (KeyValuePair<UnitRequest, int> entry in _model.Reserve)
            {
                UnitRequestKey candidate = UnitRequestKey.From(entry.Key);
                if (candidate.Kind != kind || candidate.Key != key) continue;
                request = entry.Key;
                return true;
            }

            request = null;
            return false;
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
