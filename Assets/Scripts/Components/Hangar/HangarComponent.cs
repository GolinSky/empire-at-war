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
    /// Launching stops for good when the hangar hardpoint or the carrier is destroyed.
    /// </summary>
    public sealed class HangarComponent : MonoComponent<HangarModel>, ITickable, ILateDisposable
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
                Launch(bay);
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

        private void Launch(int bay)
        {
            ISquadron squadron = _squadronFactory.Create(_playerType, _data.HangarBays[bay].SquadronType,
                launchPoint.position, launchPoint.rotation);
            Action handler = null;
            handler = () =>
            {
                squadron.Released -= handler;
                _launched.Remove((squadron, handler));
                Model.SquadronLost(bay);
            };
            squadron.Released += handler;
            _launched.Add((squadron, handler));
            squadron.Guard(_carrier.Value, Vector3.zero);
        }
    }
}
