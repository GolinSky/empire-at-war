using System;
using EmpireAtWar.Models.Skirmish;
using LightWeightFramework.Model;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ISkirmishCameraModelObserver: IModelObserver
    {
        event Action<Vector3> OnTranslateDirectionChanged;
        event Action<Vector3> OnPositionChanged;
        Vector3 MapSize { get; }
        
    }
    [CreateAssetMenu(fileName = "SkirmishCameraModel", menuName = "Model/SkirmishCameraModel")]
    public class SkirmishCameraModel:Model, ISkirmishCameraModelObserver, IInitializable
    {
        public event Action<Vector3> OnTranslateDirectionChanged;
        public event Action<Vector3> OnPositionChanged;
        [field:SerializeField] public Vector3 MapSize { get; private set; }

        public Vector3 TranslateDirection
        {
            set => OnTranslateDirectionChanged?.Invoke(value);
        }
        public Vector3 CameraPosition
        {
            set => OnPositionChanged?.Invoke(value);
        }

        [Inject]
        public SkirmishGameData SkirmishGameData { get; }
        public void Initialize()
        {
            
        }
    }
}