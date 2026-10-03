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
        private bool _isSelected;

        public event Action<bool> OnSelected;

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
