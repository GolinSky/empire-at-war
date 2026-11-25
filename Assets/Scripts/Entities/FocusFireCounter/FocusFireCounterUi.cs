using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.FocusFireCounter
{
    /// <summary>
    /// Focus fire slot counter below a target: the local team's ship and squadron holders over the cap. A pool
    /// without a cap is hidden; a full pool uses the warning colour.
    /// </summary>
    public sealed class FocusFireCounterUi : BaseUi
    {
        private const string SHIPS_PREFIX = "SHIPS ";
        private const string SQUADRONS_PREFIX = "SQD ";

        [SerializeField] private RectTransform screenRect;
        [SerializeField] private RectTransform anchor;
        [SerializeField] private TMP_Text shipsLabel;
        [SerializeField] private TMP_Text squadronsLabel;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, -40f);
        [SerializeField] private Color openColor = new Color(0.42745098f, 0.72156864f, 0.8862745f, 1f);
        [SerializeField] private Color fullColor = new Color(0.9098039f, 0.43137255f, 0.42352942f, 1f);

        private (int Count, int Cap) _ships = (-1, -1);
        private (int Count, int Cap) _squadrons = (-1, -1);

        public void Initialize() => HideCounts();

        public void ShowCounts(Vector2 screenPosition, int ships, int shipCap, int squadrons, int squadronCap)
        {
            // The owning feedback canvas is Screen Space Overlay.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screenRect, screenPosition + screenOffset, null,
                out Vector2 position);
            anchor.anchoredPosition = position;
            if (!anchor.gameObject.activeSelf) anchor.gameObject.SetActive(true);
            SetPool(shipsLabel, SHIPS_PREFIX, ships, shipCap, ref _ships);
            SetPool(squadronsLabel, SQUADRONS_PREFIX, squadrons, squadronCap, ref _squadrons);
        }

        public void HideCounts()
        {
            if (anchor.gameObject.activeSelf) anchor.gameObject.SetActive(false);
        }

        // Text is rebuilt only when a value changes; the counter is positioned every frame.
        private void SetPool(TMP_Text label, string prefix, int count, int cap, ref (int Count, int Cap) shown)
        {
            bool isCapped = cap > 0;
            if (label.gameObject.activeSelf != isCapped) label.gameObject.SetActive(isCapped);
            if (!isCapped || shown == (count, cap)) return;
            shown = (count, cap);
            label.text = prefix + count + "/" + cap;
            label.color = count >= cap ? fullColor : openColor;
        }
    }
}
