using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.Loading
{
    public sealed class LoadingUi : BaseUi
    {
        private const float ROTATION_SPEED = 180f;

        [SerializeField] private RectTransform loadingIcon;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject sceneCamera;

        public void RenderStatus(string status) => statusText.text = status;

        public void HideSceneCamera() => sceneCamera.SetActive(false);

        private void Update()
        {
            loadingIcon.Rotate(0f, 0f, -ROTATION_SPEED * Time.unscaledDeltaTime);
        }
    }
}
