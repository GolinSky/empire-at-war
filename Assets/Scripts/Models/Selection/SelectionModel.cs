using System;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Selection
{
    public interface ISelectionModelObserver : IModelObserver
    {
        event Action<bool> OnSelected;
        bool IsSelected { get; }
    }

    [CreateAssetMenu(fileName = "SelectionModel", menuName = "Model/SelectionModel")]
    public class SelectionModel : Model, ISelectionModelObserver
    {
        public event Action<bool> OnSelected;
        private bool isSelected;
        
        public bool IsSelected
        {
            set
            {
                isSelected = value;
                OnSelected?.Invoke(isSelected);
            }
            get => isSelected;
        }
    }
}