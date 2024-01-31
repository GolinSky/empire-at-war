using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Selection
{
    public interface ISelectionModelObserver : IModelObserver
    {
        event Action<bool> OnSelected;
    }

    [CreateAssetMenu(fileName = "SelectionModel", menuName = "Model/SelectionModel")]
    public class SelectionModel : Model, ISelectionModelObserver
    {
        public event Action<bool> OnSelected;
        
        public bool IsSelected
        {
            set => OnSelected?.Invoke(value);
        }
    }
}