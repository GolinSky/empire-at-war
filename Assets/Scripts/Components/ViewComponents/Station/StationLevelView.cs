using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Station
{
    public sealed class StationLevelView : MonoBehaviour
    {
        [Tooltip("Explicit model mapping: element 0 is level 1, through element 4 for level 5.")]
        [SerializeField] private StationLevelModel[] levelModels;
        [SerializeField] private BoxCollider hullCollider;
        [Tooltip("Gameplay hardpoints; each model's mounts place them by hardpoint id.")]
        [SerializeField] private HardPoint[] hardPoints;
        [SerializeField] private Transform launchPoint;
        [SerializeField] private RectTransform selectionMarker;
        [SerializeField] private Shield shield;
        [SerializeField] private MeshFilter shieldMesh;

        private HardPointHealthObserver[] _observers;

        public StationLevelModel CurrentModel { get; private set; }

        private void Awake()
        {
            _observers = new HardPointHealthObserver[hardPoints.Length];
            for (int index = 0; index < hardPoints.Length; index++)
            {
                _observers[index] = new HardPointHealthObserver(hardPoints[index].Id, HandleHardPointHealth);
                ((INotifier<float>)hardPoints[index]).AddObserver(_observers[index]);
            }
        }

        private void OnDestroy()
        {
            for (int index = 0; index < hardPoints.Length; index++)
            {
                ((INotifier<float>)hardPoints[index]).RemoveObserver(_observers[index]);
            }
        }

        public void ApplyLevel(int level)
        {
            StationLevelModel next = levelModels[level - 1];
            foreach (StationLevelModel model in levelModels)
            {
                model.gameObject.SetActive(model == next);
            }

            CurrentModel = next;
            Bounds bounds = next.HullBounds;
            hullCollider.center = bounds.center;
            hullCollider.size = bounds.size;
            foreach (StationMount mount in next.Mounts)
            {
                HardPoint hardPoint = GetHardPoint(mount.HardPointId);
                hardPoint.transform.position = mount.Point.position;
                if (mount.Art != null) mount.Art.SetActive(!hardPoint.IsDestroyed);
            }

            launchPoint.position = next.LaunchExit.position;
            selectionMarker.localPosition = new Vector3(bounds.center.x, bounds.min.y - 2f, bounds.center.z);
            selectionMarker.sizeDelta = new Vector2(bounds.size.x, bounds.size.z);
            shield.transform.localPosition = next.ShieldCenter;
            shield.SetHull(shieldMesh, next.ShieldMesh, next.ShieldPlanes);
        }

        private void HandleHardPointHealth(int hardPointId, float healthPercentage)
        {
            if (CurrentModel == null) return;
            foreach (StationMount mount in CurrentModel.Mounts)
            {
                if (mount.HardPointId == hardPointId && mount.Art != null) mount.Art.SetActive(healthPercentage > 0f);
            }
        }

        private HardPoint GetHardPoint(int id)
        {
            foreach (HardPoint hardPoint in hardPoints)
            {
                if (hardPoint.Id == id) return hardPoint;
            }

            throw new System.ArgumentException($"{name} has no hardpoint with id {id}.");
        }
    }
}
