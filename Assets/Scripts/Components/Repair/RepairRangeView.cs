using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Repair;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Repair
{
    /// <summary>World ring of the repair radius, shown while the repairer is selected (only the local player's own units).</summary>
    public sealed class RepairRangeView : MonoBehaviour
    {
        private const int SEGMENTS = 128;

        [SerializeField] private LineRenderer ring;
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
            if (isSelected) Draw();
            ring.enabled = isSelected;
        }

        // World space: the radius is measured in world units from the repairer, independent of the prefab scale.
        private void Draw()
        {
            Vector3 center = transform.position;
            ring.positionCount = SEGMENTS;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2f / SEGMENTS;
                ring.SetPosition(i, center + new Vector3(Mathf.Sin(angle) * _radius, 0f, Mathf.Cos(angle) * _radius));
            }
        }
    }
}
