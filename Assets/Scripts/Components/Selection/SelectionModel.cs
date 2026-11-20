using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Models.Selection
{
    public interface ISelectionModelObserver : IModelObserver
    {
        event Action<bool> OnSelected;

        bool IsSelected { get; }
    }

    public class SelectionModel : Model, ISelectionModelObserver
    {
        private bool _isSelected;

        public event Action<bool> OnSelected;

        public bool IsSelected
        {
            set
            {
                _isSelected = value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // TEMPORARY selection-cost probe: one profiler sample per handler type. Remove after the investigation.
                if (OnSelected == null) return;
                foreach (Delegate handler in OnSelected.GetInvocationList())
                {
                    UnityEngine.Profiling.Profiler.BeginSample("SelectionProbe.OnSelected." + handler.Method.DeclaringType.Name);
                    ((Action<bool>)handler)(_isSelected);
                    UnityEngine.Profiling.Profiler.EndSample();
                }
#else
                OnSelected?.Invoke(_isSelected);
#endif
            }
            get => _isSelected;
        }
    }
}
