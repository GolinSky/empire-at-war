using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Loading
{
    public sealed class LoadingUi : BaseUi
    {
        private const float ROTATION_SPEED = 180f;

        [SerializeField] private RectTransform loadingIcon;

        private void Update()
        {
            loadingIcon.Rotate(0f, 0f, -ROTATION_SPEED * Time.unscaledDeltaTime);
        }
    }
}
