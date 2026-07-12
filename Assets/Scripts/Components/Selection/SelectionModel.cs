using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Selection
{
    public interface ISelectionModelObserver : IModelObserver
    {
        event Action<bool> OnSelected;
        bool IsSelected { get; }
    }

    public class SelectionModel : PureModel, ISelectionModelObserver
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
