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
        event Action<MarkData> OnMarkAdded;
        event Action<MiniMapMarker> OnMarkerAdded;
        event Action<MiniMapMarker> OnMarkerRemoved;
        
        MarkView MarkViewPrefab { get;}
        Vector2Range MapRange { get; }
        MarkData PlayerBase { get; }
        MarkData EnemyBase { get; }
        CameraMarkData CameraMark { get;}
        IReadOnlyList<MiniMapMarker> Markers { get; }
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
        public MarkData PlayerBase { get; private set; }
        public MarkData EnemyBase { get; private set; }
        public CameraMarkData CameraMark { get; } = new CameraMarkData();
        public IReadOnlyList<MiniMapMarker> Markers => _markers;

        private readonly List<MiniMapMarker> _markers = new List<MiniMapMarker>();

        [field:SerializeField] public DictionaryWrapper<MarkType, Sprite> MarkWrapper { get; private set; }
        [field:SerializeField] public MarkView MarkViewPrefab { get; private set; }

        public bool IsInputBlocked { get; set; }

        public void AddMark(MarkType markType, Vector3 position)
        {
            Sprite icon = GetIcon(markType);
            switch (markType)
            {
                case MarkType.PlayerBase:
                    PlayerBase = new MarkData(position, icon);
                    return;
                case MarkType.EnemyBase:
                    EnemyBase = new MarkData(position, icon);
                    return;
            }
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
        
        public Sprite GetIcon(MarkType markType) => MarkWrapper.Dictionary[markType];
    }
}
