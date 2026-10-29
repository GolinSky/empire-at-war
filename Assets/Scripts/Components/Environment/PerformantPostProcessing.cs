using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Environment
{
    public sealed class PerformantPostProcessing : MonoBehaviour
    {
        private const int PERFORMANT_QUALITY_LEVEL = 0;

        [SerializeField] private Volume volume;

        private int _qualityLevel = -1;

        private void OnEnable() => ApplyQuality();

        private void LateUpdate()
        {
            if (_qualityLevel != QualitySettings.GetQualityLevel()) ApplyQuality();
        }

        private void ApplyQuality()
        {
            _qualityLevel = QualitySettings.GetQualityLevel();
            volume.enabled = _qualityLevel == PERFORMANT_QUALITY_LEVEL;
        }

        private void OnDisable() => volume.enabled = false;
    }
}
