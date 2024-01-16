using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ISkirmishCameraModelObserver: IModelObserver
    {
        Vector3 MapSize { get; }
    }
    [CreateAssetMenu(fileName = "SkirmishCameraModel", menuName = "Model/SkirmishCameraModel")]
    public class SkirmishCameraModel:Model, ISkirmishCameraModelObserver
    {
        [field:SerializeField] public Vector3 MapSize { get; private set; }
    }
}