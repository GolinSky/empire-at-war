using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.SkirmishCamera
{
    public interface ISkirmishCameraModelObserver: IModelObserver
    {
        
    }
    [CreateAssetMenu(fileName = "SkirmishCameraModel", menuName = "Model/SkirmishCameraModel")]
    public class SkirmishCameraModel:Model, ISkirmishCameraModelObserver
    {
        
    }
}