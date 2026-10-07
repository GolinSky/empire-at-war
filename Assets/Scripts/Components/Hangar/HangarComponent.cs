using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>
    /// Launches squadrons from a carrier's or space station's reserve and escorts them around it.
    /// Reserve launching stops for good when the hangar hardpoint or its owner is destroyed.
    /// <see cref="Launch"/> launches extra squadrons outside the reserve.
    /// </summary>
    public sealed class HangarComponent : MonoComponent<HangarModel>, IHangarCommand, IInitializable, ITickable,
        ILateDisposable
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
        private readonly List<(ISquadron Squadron, Action Handler)> _launched =
            new List<(ISquadron Squadron, Action Handler)>();
        private SquadronFactory _squadronFactory;
        private LazyInject<IEntity> _carrier;

        private PlayerId _owner;

        private bool _isReleased;

        [Inject]
        private void Construct(IHangarData data, IHealthModelObserver health, HangarModel model,
            SquadronFactory squadronFactory, LazyInject<IEntity> carrier, PlayerId owner)
        {
            SetModel(model);
            _data = data;
            _squadronFactory = squadronFactory;
            _owner = owner;
            _carrier = carrier;
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
                int bay = Array.FindIndex(bayHardPoints, point => point.transform == unit.Transform);
                if (bayHardPoints.Length != 0 && bay < 0)
                {
                    continue;
                }
                Action handler = bayHardPoints.Length == 0 ? OnHangarDestroyed : () => Model.DisableBay(bay);
                unit.OnDestroyed += handler;
                _hangarUnits.Add((unit, handler));
            }
            if (!hasLaunchHangar || (bayHardPoints.Length != 0 && _hangarUnits.Count != Model.BayCount))
            {
                throw new InvalidOperationException($"{name}: hangar hardpoints must belong to the health hardpoints.");
            }
        }

        public void LateDispose() => Release();

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            if (Model.TryLaunch(Time.deltaTime, out int bay))
            {
                LaunchFromBay(bay);
            }
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            if (isDestroyable)
            {
                foreach ((IHardPointModel unit, Action handler) in _hangarUnits)
                {
                    unit.OnDestroyed -= handler;
                }
                _hangarUnits.Clear();
            }
            Model.Shutdown();
            foreach ((ISquadron squadron, Action handler) in _launched)
            {
                squadron.Released -= handler;
            }

            _launched.Clear();
        }

        public ISquadron Launch(SquadronType squadronType) => Launch(squadronType, launchPoint);

        private ISquadron Launch(SquadronType squadronType, Transform point)
        {
            ISquadron squadron = _squadronFactory.Create(_owner, squadronType,
                point.position, point.rotation);
            squadron.Guard(_carrier.Value, Vector3.zero);
            return squadron;
        }

        private void OnHangarDestroyed()
        {
            foreach ((IHardPointModel unit, Action handler) in _hangarUnits)
            {
                if (!unit.IsDestroyed)
                {
                    return;
                }
            }
            Model.Shutdown();
        }

        private void LaunchFromBay(int bay)
        {
            Transform point = bayLaunchPoints.Length == 0 ? launchPoint : bayLaunchPoints[bay];
            ISquadron squadron = Launch(_data.HangarBays[bay].SquadronType, point);
            Action handler = null;
            handler = () =>
            {
                squadron.Released -= handler;
                _launched.Remove((squadron, handler));
                Model.SquadronLost(bay);
            };
            squadron.Released += handler;
            _launched.Add((squadron, handler));
        }
    }
}
