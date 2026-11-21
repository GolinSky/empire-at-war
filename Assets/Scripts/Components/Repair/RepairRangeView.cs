using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Repair;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Repair
{
    /// <summary>Flat preview of the repair radius, shown while the repairer is selected (only the local player's own units).</summary>
    public sealed class RepairRangeView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer ring;
        private ISelectionModelObserver _selection;
        private float _radius;

        [Inject]
        private void Construct(ISelectionModelObserver selection, RepairProfile profile)
        {
            _selection = selection;
            _radius = profile.Radius;
            _selection.OnSelected += HandleSelected;
            HandleSelected(_selection.IsSelected);
        }

        private void OnDestroy()
        {
            _selection.OnSelected -= HandleSelected;
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
