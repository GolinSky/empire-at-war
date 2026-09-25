using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>
    /// Launches squadrons from a carrier's reserve and escorts them around the carrier.
    /// Reserve launching stops for good when the hangar hardpoint or the carrier is destroyed.
    /// A hangar without bays (space stations) only launches squadrons requested through <see cref="Launch"/>.
    /// </summary>
    public sealed class HangarComponent : MonoComponent<HangarModel>, IHangarCommand, ITickable, ILateDisposable
    {
        [SerializeField] private Transform launchPoint;
        [SerializeField] private HardPoint hangarHardPoint;

        private readonly List<(ISquadron Squadron, Action Handler)> _launched =
            new List<(ISquadron Squadron, Action Handler)>();
        private IHangarData _data;
        private SquadronFactory _squadronFactory;
        private PlayerType _playerType;
        private LazyInject<IEntity> _carrier;
        private bool _isReleased;

        [Inject]
        private void Construct(HangarModel model, IHangarData data, SquadronFactory squadronFactory,
            PlayerType playerType, LazyInject<IEntity> carrier)
        {
            SetModel(model);
            _data = data;
            _squadronFactory = squadronFactory;
            _playerType = playerType;
            _carrier = carrier;
        }

        public void Tick()
        {
            if (_isReleased)
            {
                return;
            }

            if (hangarHardPoint.IsDestroyed)
            {
                Model.Shutdown();
            }

            if (Model.TryLaunch(Time.deltaTime, out int bay))
            {
                LaunchFromBay(bay);
            }
        }

        public void LateDispose() => Release();

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            Model.Shutdown();
            foreach ((ISquadron squadron, Action handler) in _launched)
            {
                squadron.Released -= handler;
            }

            _launched.Clear();
        }

        public ISquadron Launch(SquadronType squadronType)
        {
            ISquadron squadron = _squadronFactory.Create(_playerType, squadronType,
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
