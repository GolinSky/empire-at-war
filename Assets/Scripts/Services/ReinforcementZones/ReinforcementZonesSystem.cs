using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ReinforcementZones
{
    public interface IReinforcementZonesSystem
    {
        event Action OwnershipChanged;

        bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position);
        bool TryGetRandomSpawnPosition(PlayerType playerType, out Vector3 position);
        bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position);
    }

    public sealed class ReinforcementZonesSystem : MonoBehaviour, IReinforcementZonesSystem, IInitializable, ITickable
    {
        [SerializeField, Min(0f)] private float _spawnEdgePadding = 3f;

        private readonly List<ReinforcementZonePresenter> _zones = new List<ReinforcementZonePresenter>();
        private IShipService _shipService;

        public event Action OwnershipChanged;

        [Inject]
        private void Construct(IShipService shipService)
        {
            _shipService = shipService;
        }

        public void Initialize()
        {
            _zones.Clear();
            ReinforcementZoneView[] views = GetComponentsInChildren<ReinforcementZoneView>(true);
            foreach (ReinforcementZoneView view in views)
            {
                ReinforcementZoneModel model = new ReinforcementZoneModel(
                    view.StartingOwner,
                    view.IsCapturable,
                    view.CaptureDuration);
                _zones.Add(new ReinforcementZonePresenter(model, view));
            }

            if (_zones.Count == 0)
            {
                Debug.LogError("ReinforcementZonesSystem requires at least one ReinforcementZoneView child.", this);
            }
        }

        public void Tick()
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                int playerShips = 0;
                int opponentShips = 0;

                foreach (IShipEntity ship in _shipService.Ships)
                {
                    if (!zone.Contains(ship.WorldPosition))
                    {
                        continue;
                    }

                    if (ship.PlayerType == PlayerType.Player)
                    {
                        playerShips++;
                    }
                    else if (ship.PlayerType == PlayerType.Opponent)
                    {
                        opponentShips++;
                    }
                }

                if (zone.Tick(Time.deltaTime, playerShips, opponentShips))
                {
                    OwnershipChanged?.Invoke();
                }
            }
        }

        public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner == playerType && zone.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRandomSpawnPosition(PlayerType playerType, out Vector3 position)
        {
            List<ReinforcementZonePresenter> ownedZones = new List<ReinforcementZonePresenter>();
            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (zone.Owner == playerType)
                {
                    ownedZones.Add(zone);
                }
            }

            if (ownedZones.Count == 0)
            {
                position = default;
                return false;
            }

            ReinforcementZonePresenter selectedZone = ownedZones[Random.Range(0, ownedZones.Count)];
            float radius = Mathf.Max(0f, selectedZone.Radius - _spawnEdgePadding);
            Vector2 offset = Random.insideUnitCircle * radius;
            position = selectedZone.Center + new Vector3(offset.x, 0f, offset.y);
            position.y = 0f;
            return true;
        }

        public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
        {
            ReinforcementZonePresenter closestZone = null;
            float closestDistance = float.MaxValue;

            foreach (ReinforcementZonePresenter zone in _zones)
            {
                if (!zone.IsCapturable || zone.Owner == playerType)
                {
                    continue;
                }

                float distance = (zone.Center - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }

            if (closestZone == null)
            {
                position = default;
                return false;
            }

            position = closestZone.Center;
            position.y = 0f;
            return true;
        }
    }
}
