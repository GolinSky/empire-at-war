using UnityEngine;

namespace EmpireAtWar.ViewComponents.Station
{
    public sealed class StationLevelView : MonoBehaviour
    {
        [Tooltip("Explicit model mapping: element 0 is level 1, through element 4 for level 5.")]
        [SerializeField] private StationLevelModel[] levelModels;
        [SerializeField] private BoxCollider hullCollider;
        [Tooltip("Existing gameplay transforms, in the same order as each model's attachment points.")]
        [SerializeField] private Transform[] attachmentTargets;
        [SerializeField] private RectTransform selectionMarker;

        public StationLevelModel CurrentModel { get; private set; }

        public void ApplyLevel(int level)
        {
            StationLevelModel next = levelModels[level - 1];
            foreach (StationLevelModel model in levelModels)
            {
                model.gameObject.SetActive(model == next);
            }

            CurrentModel = next;
            Bounds bounds = next.HullBounds;
            hullCollider.center = bounds.center;
            hullCollider.size = bounds.size;
            for (int index = 0; index < attachmentTargets.Length; index++)
            {
                attachmentTargets[index].position = next.AttachmentPoints[index].position;
            }

            selectionMarker.localPosition = new Vector3(bounds.center.x, bounds.min.y - 2f, bounds.center.z);
            selectionMarker.sizeDelta = new Vector2(bounds.size.x, bounds.size.z);
        }
    }
}
