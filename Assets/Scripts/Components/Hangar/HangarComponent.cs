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
        private IHardPointModel _hangarUnit;

        [SerializeField] private Transform launchPoint;
        [SerializeField] private HardPoint hangarHardPoint;
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
            if (!isDestroyable)
            {
                return;
            }

            foreach (IHardPointModel unit in _health.GetShipUnits(HardPointType.Any))
            {
                if (unit.Transform == hangarHardPoint.transform)
                {
                    _hangarUnit = unit;
                }
            }

            if (_hangarUnit == null)
            {
                throw new InvalidOperationException($"{name}: hangar hardpoint is not one of the health hardpoints.");
            }

            _hangarUnit.OnDestroyed += Model.Shutdown;
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
                _hangarUnit.OnDestroyed -= Model.Shutdown;
            }
            Model.Shutdown();
            foreach ((ISquadron squadron, Action handler) in _launched)
            {
                squadron.Released -= handler;
            }

            _launched.Clear();
        }

        public ISquadron Launch(SquadronType squadronType)
        {
            ISquadron squadron = _squadronFactory.Create(_owner, squadronType,
                launchPoint.position, launchPoint.rotation);
            squadron.Guard(_carrier.Value, Vector3.zero);
            return squadron;
        }

        private void LaunchFromBay(int bay)
        {
            ISquadron squadron = Launch(_data.HangarBays[bay].SquadronType);
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
