using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Menu
{
    public interface IMenuModelModelObserver:IModelObserver
    {
        
    }
    [CreateAssetMenu(fileName = "MenuModel", menuName = "Model/MenuModel")]
    public class MenuModel:Model, IMenuModelModelObserver
    {
        
    }
}