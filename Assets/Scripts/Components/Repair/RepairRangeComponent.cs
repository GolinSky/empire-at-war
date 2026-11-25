using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Repair;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Repair
{
    /// <summary>Flat preview of the repair radius, shown while the repairer is selected (only the local player's own units).</summary>
    public sealed class RepairRangeComponent : MonoBehaviour, IMonoComponent, IInitializable
    {
        [SerializeField] private MeshRenderer ring;
        private ISelectionModelObserver _selection;
        private float _radius;
        private bool _isReleased = true;

        [Inject]
        private void Construct(ISelectionModelObserver selection, RepairProfile profile)
        {
            _selection = selection;
            _radius = profile.Radius;
        }

        public void Initialize()
        {
            _isReleased = false;
            _selection.OnSelected += HandleSelected;
            HandleSelected(_selection.IsSelected);
        }

        public void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            _selection.OnSelected -= HandleSelected;
            ring.enabled = false;
        }

        private void HandleSelected(bool isSelected)
        {
            if (isSelected)
            {
                // The satellite prefab is scaled, but the repair radius is in world units.
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(_radius / parentScale.x, 1f, _radius / parentScale.z);
            }

            ring.enabled = isSelected;
        }
    }
}
