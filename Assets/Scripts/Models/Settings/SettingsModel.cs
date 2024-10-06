using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Settings
{
    public interface ISettingsModelObserver:IModelObserver
    {
        
    }
    [CreateAssetMenu(fileName = "SettingsModel", menuName = "Model/SettingsModel")]
    public class SettingsModel:Model, ISettingsModelObserver
    {
        
    }
}