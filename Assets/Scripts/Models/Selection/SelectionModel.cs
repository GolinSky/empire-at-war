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
        private bool _isSelected;
        
        public bool IsSelected
        {
            set
            {
                _isSelected = value;
                OnSelected?.Invoke(_isSelected);
            }
            get => _isSelected;
        }
    }
}