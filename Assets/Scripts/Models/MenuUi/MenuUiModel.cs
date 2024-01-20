using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.MenuUi
{
    public interface IMenuUiModel : IModelObserver
    {
    }

    [CreateAssetMenu(fileName = "MenuUiModel", menuName = "Model/MenuUiModel")]
    public class MenuUiModel : Model, IMenuUiModel
    {
        
    }
}