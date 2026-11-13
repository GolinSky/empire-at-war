using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>
    /// Launches squadrons from a carrier's or space station's reserve and escorts them around it.
    /// Reserve launching pauses while its hangar hardpoints are destroyed and stops when its owner is destroyed.
    /// <see cref="Launch"/> launches extra squadrons outside the reserve.
    /// </summary>
    public sealed class HangarComponent : MonoComponent<HangarModel>, IHangarCommand, IInitializable, ITickable,
        ILateDisposable, IHangarLaunchEvents, IHangarSaveFacade, IHangarAirWingFacade
    {
        private IHangarData _data;
        private IHealthModelObserver _health;
        private readonly List<(IHardPointModel Unit, Action Handler)> _hangarUnits =
            new List<(IHardPointModel Unit, Action Handler)>();

        [SerializeField] private Transform launchPoint;
        [SerializeField] private HardPoint hangarHardPoint;
        [SerializeField] private HardPoint[] bayHardPoints = Array.Empty<HardPoint>();
        [SerializeField] private Transform[] bayLaunchPoints = Array.Empty<Transform>();
        [SerializeField] private bool isDestroyable = true;
        private readonly List<(ISquadron Squadron, int Bay, Action Handler)> _launched =
            new List<(ISquadron Squadron, int Bay, Action Handler)>();
        private ISquadronSpawner _squadronSpawner;
        private ISquadronLaunchSlots _launchSlots;

        public event Action<ISquadron> SquadronLaunched;

        private PlayerId _owner;
        private long _carrierId;

        private bool _isReleased;

        [Inject]
        private void Construct(IHangarData data, IHealthModelObserver health, HangarModel model,
            ISquadronSpawner squadronSpawner, ISquadronLaunchSlots launchSlots,
            PlayerId owner, long entityId)
        {
            SetModel(model);
            _data = data;
            _squadronSpawner = squadronSpawner;
            _launchSlots = launchSlots;
            _owner = owner;
            _carrierId = entityId;
            _health = health;
        }

        public void Initialize()
        {
            if (bayLaunchPoints.Length != 0 && bayLaunchPoints.Length != Model.BayCount)
            {
                throw new InvalidOperationException($"{name}: launch points must match the bay count.");
            }
            if (!isDestroyable)
            {
                return;
            }
            if (bayHardPoints.Length != 0 && bayHardPoints.Length != Model.BayCount)
            {
                throw new InvalidOperationException($"{name}: hangar hardpoints must match the bay count.");
            }

            bool hasLaunchHangar = false;
            foreach (IHardPointModel unit in _health.GetShipUnits(HardPointType.Hangar))
            {
                hasLaunchHangar |= unit.Transform == hangarHardPoint.transform;
                if (bayHardPoints.Length == 0)
                {
                    TrackHangarHealth(unit, OnHangarHealthChanged);
                    continue;
                }

                for (int bay = 0; bay < bayHardPoints.Length; bay++)
                {
                    if (bayHardPoints[bay].transform != unit.Transform) continue;

                    int linkedBay = bay;
                    TrackHangarHealth(unit, () =>
                    {
                        bool wasOperational = Model.IsOperational;
                        if (unit.IsDestroyed) Model.DisableBay(linkedBay);
                        else Model.EnableBay(linkedBay);
                        RecordHangar(unit.IsDestroyed ? "bayDisabled" : "bayEnabled", linkedBay);
                        RecordOperationalChange(wasOperational);
                    });
                }
            }
            if (!hasLaunchHangar || (bayHardPoints.Length != 0 && _hangarUnits.Count != Model.BayCount))
            {
                throw new InvalidOperationException($"{name}: hangar hardpoints must belong to the health hardpoints.");
            }
        }

        private void TrackHangarHealth(IHardPointModel unit, Action handler)
        {
            unit.OnHardPointHealthChanged += handler;
            _hangarUnits.Add((unit, handler));
        }

        public void LateDispose() => Release();

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            if (!_launchSlots.IsAvailable)
            {
                Model.AdvanceCooldown(Time.deltaTime);
                return;
            }

            if (Model.TryLaunch(Time.deltaTime, out int bay))
            {
                _launchSlots.Take();
                LaunchFromBay(bay, "automatic");
            }
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            RecordHangar("hangarReleased", -1);
            if (isDestroyable)
            {
                foreach ((IHardPointModel unit, Action handler) in _hangarUnits)
                {
                    unit.OnHardPointHealthChanged -= handler;
                }
                _hangarUnits.Clear();
            }
            Model.Shutdown();
            foreach ((ISquadron squadron, int _, Action handler) in _launched)
            {
                squadron.Released -= handler;
            }

            _launched.Clear();
        }

        public ISquadron Launch(SquadronType squadronType) => Launch(squadronType, launchPoint);

        public HangarSave Capture()
        {
            int[] reserve = new int[Model.BayCount];
            for (int i = 0; i < reserve.Length; i++) reserve[i] = Model.GetReserve(i);
            LaunchedSquadronSave[] launched = new LaunchedSquadronSave[_launched.Count];
            for (int i = 0; i < launched.Length; i++)
                launched[i] = LaunchedSquadronSave.Create(_launched[i].Squadron.EntityId, _launched[i].Bay);
            return HangarSave.Create(reserve, Model.Cooldown, launched);
        }

        /// <summary>Loads reserves and the launch timer, then takes back each launched squadron that was restored.</summary>
        public void Restore(HangarSave save, IReadOnlyDictionary<long, Squadron> squadrons)
        {
            Model.Restore(save.Reserve, save.Cooldown);
            foreach (LaunchedSquadronSave launched in save.Launched)
            {
                if (!squadrons.TryGetValue(launched.SquadronId, out Squadron squadron))
                    throw new InvalidOperationException($"Launched squadron {launched.SquadronId} was not restored.");
                Model.AdoptLaunched(launched.Bay);
                Track(squadron, launched.Bay);
            }
        }

        public bool CanLaunch => !_isReleased && Model.IsOperational;

        public int LaunchedCount => _launched.Count;

        public ISquadron GetLaunched(int index) => _launched[index].Squadron;

        public bool CanLaunchReplacement =>!_isReleased && _launchSlots.IsAvailable && Model.CanLaunchReplacement;

        public bool TryLaunchReplacement()
        {
            if (!CanLaunchReplacement || !Model.TryLaunchReplacement(out int bay)) return false;
            _launchSlots.Take();
            LaunchFromBay(bay, "replacement");
            return true;
        }

        public bool CanLaunchImmediately => !_isReleased && Model.CanLaunchImmediately;

        public bool TryLaunchImmediately()
        {
            if (!CanLaunchImmediately || !_launchSlots.IsAvailable || !Model.TryLaunchImmediately(out int bay)) return false;
            _launchSlots.Take();
            LaunchFromBay(bay, "immediate");
            return true;
        }

        private ISquadron Launch(SquadronType squadronType, Transform point)
        {
            ISquadron squadron = _squadronSpawner.Spawn(_owner, squadronType,
                point.position, point.rotation);
            SquadronLaunched?.Invoke(squadron);
            return squadron;
        }

        private void OnHangarHealthChanged()
        {
            bool wasOperational = Model.IsOperational;
            bool hasLiveHangar = false;
            foreach ((IHardPointModel unit, Action handler) in _hangarUnits)
                hasLiveHangar |= !unit.IsDestroyed;
            if (hasLiveHangar) Model.Resume();
            else Model.Shutdown();
            RecordOperationalChange(wasOperational);
        }

        private void LaunchFromBay(int bay, string source)
        {
            Transform point = bayLaunchPoints.Length == 0 ? launchPoint : bayLaunchPoints[bay];
            ISquadron squadron = Launch(_data.HangarBays[bay].SquadronType, point);
            Track(squadron, bay);
            SquadronTelemetry.Hangar("launch", _owner, _carrierId, bay, squadron.EntityId,
                _data.HangarBays[bay].SquadronType.ToString(), point.position,
                $"source={source};reserve={Model.GetReserve(bay)};active={Model.GetActive(bay)}");
        }

        private void RecordOperationalChange(bool wasOperational)
        {
            if (wasOperational != Model.IsOperational)
                RecordHangar(Model.IsOperational ? "hangarResumed" : "hangarShutdown", -1);
        }

        private void RecordHangar(string kind, int bay)
        {
            int reserve = 0;
            for (int i = 0; i < Model.BayCount; i++) reserve += Model.GetReserve(i);
            SquadronTelemetry.Hangar(kind, _owner, _carrierId, bay, 0, "", transform.position,
                $"reserve={reserve};launched={_launched.Count}");
        }

        // Losing a bay's squadron frees the bay for a replacement.
        private void Track(ISquadron squadron, int bay)
        {
            Action handler = null;
            handler = () =>
            {
                squadron.Released -= handler;
                _launched.Remove((squadron, bay, handler));
                Model.SquadronLost(bay);
                SquadronTelemetry.Hangar("squadronLost", _owner, _carrierId, bay, squadron.EntityId,
                    _data.HangarBays[bay].SquadronType.ToString(), squadron.WorldPosition,
                    $"active={Model.GetActive(bay)};reserve={Model.GetReserve(bay)}");
            };
            squadron.Released += handler;
            _launched.Add((squadron, bay, handler));
        }
    }
}
