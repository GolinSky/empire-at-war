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
        // A right-drag shorter than this share of the preview's planar radius keeps the heading.
        private const float AIM_DEAD_ZONE_FACTOR = 0.25f;
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
        // Lives as long as the battle, so a manual heading carries over to the next placement.
        private readonly ReinforcementFacing _facing = new ReinforcementFacing();
        private Quaternion _previewRotation = Quaternion.identity;
        // Set by the first right-drag; the previews stay here until the drop or a facing reset.
        private bool _isPinned;
        private Vector3 _aimAnchor;

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

            Vector3 cursor = _cameraService.GetWorldPoint(_pointerInput.Position, _previews[0].Position);
            cursor.y = 0;
            Vector3 position = UpdateFacing(cursor);
            bool canPlace = !_inputLock.IsLocked && !_uiHitTest.IsOverUi(_pointerInput.Position) &&
                            !IsPlacementCapacityBlocked();
            bool hasValidSlot = false;
            for (int i = 0; i < _previews.Count; i++)
            {
                Vector3 slot = GetSlotPosition(position, i);
                _previews[i].UpdatePosition(slot);
                if (_placement.IsRotatable) _previews[i].SetRotation(_previewRotation);
                _previews[i].SetPlacementValidity(canPlace && _placement.IsPositionValid(slot, _previewRotation));
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
                case SecondStationUnitRequest secondStationUnitRequest:
                    _model.AddReinforcement(secondStationUnitRequest);
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
                _isPinned = false;
                // Ship hull cells in the overlay keep the station heading; the preview tint checks the chosen one.
                _spawnAreaOverlay.Show(_owner.Id, request,
                    position => placement.IsPositionValid(position, _previewRotation));
                _slotSpacing = _spawnAreaOverlay.GetPlacementSpacing(_slotSpacing);
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
                // A pinned preview drops where it was pinned, not under the cursor, at the usual drop height.
                if (_isPinned) dropPoint = new Vector3(_aimAnchor.x, dropPoint.y, _aimAnchor.z);
                Quaternion rotation = GetFacingRotation(dropPoint);
                // Previews flag locally blocked slots; the authority re-checks every slot it receives.
                List<Vector3> slots = new List<Vector3>(_previews.Count);
                for (int i = 0; i < _previews.Count; i++)
                {
                    Vector3 slot = GetSlotPosition(dropPoint, i);
                    _previews[i].SetPlacementValidity(_placement.IsPositionValid(slot, rotation));
                    if (_previews[i].CanSpawn) slots.Add(slot);
                }

                if (slots.Count == 0)
                {
                    PlacementRejected?.Invoke();
                    return;
                }

                UnitRequestKey key = UnitRequestKey.From(_currentRequest);
                _commands.Submit(new DeployReinforcementCommand(_owner.Id, key.Kind, key.Key, slots.ToArray(),
                    rotation.eulerAngles.y));
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
            Quaternion rotation = Quaternion.Euler(0f, command.Yaw, 0f);
            foreach (Vector3 slot in command.Slots)
            {
                if (_model.GetReserveCount(request) == 0) break;
                if (!placement.IsPositionValid(slot, rotation)) continue;
                if (usesCapacity && !_model.HasRoomFor(request))
                {
                    isCapacityBlocked = true;
                    break;
                }

                placement.Spawn(slot, rotation);
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

        /// <summary>Applies this frame's turn, aim and reset input; returns where the previews stand.</summary>
        private Vector3 UpdateFacing(Vector3 cursor)
        {
            if (!_placement.IsRotatable) return cursor;

            if (_deployInput.WasFacingResetPressed)
            {
                // Reset also unpins, so the previews follow the cursor again.
                _facing.Reset();
                _isPinned = false;
            }

            if (_deployInput.IsAimHeld)
            {
                // Right-drag pins the previews and turns them toward the cursor; a later right-drag
                // keeps the same pin so releasing and pressing again only fine-tunes the heading.
                if (!_isPinned) _aimAnchor = cursor;
                _isPinned = true;
                _facing.Aim(_aimAnchor, cursor, _previews[0].PlanarRadius * AIM_DEAD_ZONE_FACTOR);
            }
            else
            {
                Vector3 turnPoint = _isPinned ? _aimAnchor : cursor;
                // Unscaled time: the turn sweep is interface input and must not stall while paused.
                _facing.Turn(_deployInput.TurnDirection, _deployInput.WasTurnPressed, Time.unscaledDeltaTime,
                    GetAutoYaw(turnPoint));
            }

            // Releasing the right button must not snap a pinned preview back under the cursor.
            Vector3 position = _isPinned ? _aimAnchor : cursor;

            _previewRotation = GetFacingRotation(position);
            return position;
        }

        // World-space yaw on the battle plane, independent of the camera angle.
        private Quaternion GetFacingRotation(Vector3 position) => _placement.IsRotatable
            ? Quaternion.Euler(0f, _facing.GetYaw(GetAutoYaw(position)), 0f)
            : Quaternion.identity;

        private float GetAutoYaw(Vector3 position) => _placement.GetAutoFacing(position).eulerAngles.y;

        private Vector3 GetSlotPosition(Vector3 dropPoint, int index)
        {
            FormationPoint slot = FormationModel.CalculateGridDestination(index, _previews.Count,
                new FormationPoint(dropPoint.x, dropPoint.z), _slotSpacing);
            return _spawnAreaOverlay.GetPlacementPosition(new Vector3(slot.X, dropPoint.y, slot.Z));
        }

        private void DestroyPreviews()
        {
            foreach (UnitSpawnView preview in _previews)
                preview.Destroy();
            _previews.Clear();
        }
    }
}
