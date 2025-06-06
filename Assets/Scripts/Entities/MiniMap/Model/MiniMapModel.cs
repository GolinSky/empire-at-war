﻿using System;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Views.MiniMap;
using UnityEngine;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.EditorSerialization;

namespace EmpireAtWar.Models.MiniMap
{
    public interface IMiniMapModelObserver : IModelObserver
    {
        event Action<bool> OnInteractableChanged;
        event Action<MarkData> OnMarkAdded;
        event Action<DynamicMarkData> OnDynamicMarkAdded;
        
        MarkView MarkViewPrefab { get;}
        Vector2Range MapRange { get; }
        MarkData PlayerBase { get; }
        MarkData EnemyBase { get; }
        DynamicMarkData CameraMark { get;}
        bool IsInputBlocked { get; }
    }

    [CreateAssetMenu(fileName = "MiniMapModel", menuName = "Model/MiniMapModel")]
    public class MiniMapModel : Model, IMiniMapModelObserver
    {
        public event Action<bool> OnInteractableChanged;
        public event Action<MarkData> OnMarkAdded;
        public event Action<DynamicMarkData> OnDynamicMarkAdded;
        public Vector2Range MapRange { get; set; }
        public MarkData PlayerBase { get; private set; }
        public MarkData EnemyBase { get; private set; }
        public DynamicMarkData CameraMark { get; private set; }

        [field:SerializeField] public DictionaryWrapper<MarkType, Sprite> MarkWrapper { get; private set; }
        [field:SerializeField] public MarkView MarkViewPrefab { get; private set; }

        public bool IsInteractive
        {
            set => OnInteractableChanged?.Invoke(value);
        }
        
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

        
        public void AddMark(MarkType markType, Transform transform)
        {
            Sprite icon = GetIcon(markType);
            if (markType == MarkType.Camera)
            {
                CameraMark = new CameraMarkData(transform.position, icon, transform);
                return;
            }
            OnMarkAdded?.Invoke(new DynamicMarkData(transform.position, icon, transform));
        }
        
        private Sprite GetIcon(MarkType markType) => MarkWrapper.Dictionary[markType];
    }
}