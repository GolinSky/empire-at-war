using System;
using System.Collections.Generic;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.MiniMap;
using UnityEngine;
using EmpireAtWar.Mvc;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.MiniMap
{
    public interface IMiniMapModelObserver : IModelObserver
    {
        event Action<MiniMapMarker> OnMarkerAdded;

        event Action<MiniMapMarker> OnMarkerRemoved;

        event Action<MiniMapAlertType, Vector3> OnAlertAdded;

        MarkView MarkViewPrefab { get;}
        Vector2Range MapRange { get; }
        CameraMarkData CameraMark { get;}
        IReadOnlyList<MiniMapMarker> Markers { get; }
        IReadOnlyList<MiniMapObstacle> Obstacles { get; }
        IReadOnlyList<MiniMapHazard> Hazards { get; }
        bool IsInputBlocked { get; }

        Sprite GetIcon(MarkType markType);
    }

    [CreateAssetMenu(fileName = nameof(MiniMapData), menuName = "Data/MiniMapData")]
    public class MiniMapData : Data, IModel, IMiniMapModelObserver
    {
        private readonly List<MiniMapMarker> _markers = new List<MiniMapMarker>();
        private readonly List<MiniMapObstacle> _obstacles = new List<MiniMapObstacle>();
        private readonly List<MiniMapHazard> _hazards = new List<MiniMapHazard>();

        public event Action<MiniMapMarker> OnMarkerAdded;

        public event Action<MiniMapMarker> OnMarkerRemoved;

        public event Action<MiniMapAlertType, Vector3> OnAlertAdded;

        public Vector2Range MapRange { get; set; }
        public CameraMarkData CameraMark { get; } = new CameraMarkData();
        public IReadOnlyList<MiniMapMarker> Markers => _markers;
        public IReadOnlyList<MiniMapObstacle> Obstacles => _obstacles;
        public IReadOnlyList<MiniMapHazard> Hazards => _hazards;

        public void AddHazard(MiniMapHazard hazard) => _hazards.Add(hazard);

        [field:SerializeField] public DictionaryWrapper<MarkType, Sprite> MarkWrapper { get; private set; }
        [field:SerializeField] public MarkView MarkViewPrefab { get; private set; }

        public bool IsInputBlocked { get; set; }

        public void AddAlert(MiniMapAlertType alertType, Vector3 worldPosition) =>
            OnAlertAdded?.Invoke(alertType, worldPosition);

        public void AddMarker(MiniMapMarker marker)
        {
            _markers.Add(marker);
            OnMarkerAdded?.Invoke(marker);
        }

        public void RemoveMarker(MiniMapMarker marker)
        {
            if (_markers.Remove(marker))
            {
                OnMarkerRemoved?.Invoke(marker);
            }
        }

        public void AddObstacle(MiniMapObstacle obstacle)
        {
            _obstacles.Add(obstacle);
        }

        public bool IsObstacleAt(Vector3 worldPoint)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                if (_obstacles[i].Contains(worldPoint.x, worldPoint.z))
                {
                    return true;
                }
            }

            return false;
        }

        public Sprite GetIcon(MarkType markType) => MarkWrapper.Dictionary[markType];
    }
}
