using System;
using EmpireAtWar.Models.Players;
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
        event Action<MarkData> OnMarkAdded;
        event Action<MiniMapMarker> OnMarkerAdded;
        event Action<MiniMapMarker> OnMarkerRemoved;
        
        MarkView MarkViewPrefab { get;}
        Vector2Range MapRange { get; }
        IReadOnlyList<BaseMarkData> Bases { get; }
        CameraMarkData CameraMark { get;}
        IReadOnlyList<MiniMapMarker> Markers { get; }
        IReadOnlyList<MiniMapObstacle> Obstacles { get; }
        bool IsInputBlocked { get; }
        Sprite GetIcon(MarkType markType);
    }

    [CreateAssetMenu(fileName = nameof(MiniMapData), menuName = "Data/MiniMapData")]
    public class MiniMapData : Data, IModel, IMiniMapModelObserver
    {
        public event Action<MarkData> OnMarkAdded;
        public event Action<MiniMapMarker> OnMarkerAdded;
        public event Action<MiniMapMarker> OnMarkerRemoved;
        public Vector2Range MapRange { get; set; }
        public IReadOnlyList<BaseMarkData> Bases => _bases;
        public CameraMarkData CameraMark { get; } = new CameraMarkData();
        public IReadOnlyList<MiniMapMarker> Markers => _markers;
        public IReadOnlyList<MiniMapObstacle> Obstacles => _obstacles;

        private readonly List<MiniMapMarker> _markers = new List<MiniMapMarker>();
        private readonly List<BaseMarkData> _bases = new List<BaseMarkData>();
        private readonly List<MiniMapObstacle> _obstacles = new List<MiniMapObstacle>();

        [field:SerializeField] public DictionaryWrapper<MarkType, Sprite> MarkWrapper { get; private set; }
        [field:SerializeField] public MarkView MarkViewPrefab { get; private set; }

        public bool IsInputBlocked { get; set; }

        // The asset outlives a battle in the Editor, so every battle starts from an empty list.
        public void ClearBases()
        {
            _bases.Clear();
        }

        public void AddBase(Vector3 position, OwnerRelation relation)
        {
            MarkType iconType = relation == OwnerRelation.Enemy ? MarkType.EnemyBase : MarkType.PlayerBase;
            _bases.Add(new BaseMarkData(position, GetIcon(iconType), relation));
        }

        public void AddMark(MarkType markType, Vector3 position)
        {
            Sprite icon = GetIcon(markType);
            OnMarkAdded?.Invoke(new MarkData(position, icon));
        }

        
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
