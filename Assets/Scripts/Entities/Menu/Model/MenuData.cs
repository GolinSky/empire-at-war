using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Models.Menu
{
    public interface IMenuModelModelObserver:IModelObserver
    {
        
    }
    [CreateAssetMenu(fileName = nameof(MenuData), menuName = "Data/MenuData")]
    public class MenuData:Data, IModel, IMenuModelModelObserver
    {
        
    }
}