using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Saves
{
    public sealed class SaveBrowserRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button removeButton;
        [SerializeField] private TMP_Text saveKind;
        [SerializeField] private TMP_Text savedAt;
        [SerializeField] private TMP_Text battleTime;
        [SerializeField] private GameObject selectionFrame;
        private Action<int> _selected;
        private Action<int> _removed;
        private int _index;

        public void Initialize(int index, string text, bool enabled, Action<int> selected, Action<int> removed)
        {
            _index = index;
            _selected = selected;
            _removed = removed;
            label.text = text;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(24, 0);
            label.rectTransform.offsetMax = new Vector2(-168, 0);
            button.interactable = enabled;
            removeButton.gameObject.SetActive(true);
            saveKind.gameObject.SetActive(false);
            savedAt.gameObject.SetActive(false);
            battleTime.gameObject.SetActive(false);
            SetSelected(false);
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
            removeButton.onClick.RemoveListener(Remove);
            removeButton.onClick.AddListener(Remove);
        }

        public void InitializeLoad(int index, string name, string kind, string saved, string duration,
            bool compatible, Action<int> selected, Action<int> removed)
        {
            Initialize(index, name, true, selected, removed);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(0.5f, 1);
            label.rectTransform.offsetMin = new Vector2(24, 48);
            label.rectTransform.offsetMax = new Vector2(-12, -10);
            removeButton.gameObject.SetActive(false);
            saveKind.gameObject.SetActive(true);
            savedAt.gameObject.SetActive(true);
            battleTime.gameObject.SetActive(true);
            saveKind.text = compatible ? kind : "INCOMPATIBLE";
            saveKind.color = compatible ? new Color32(210, 169, 94, 255) : new Color32(232, 110, 108, 255);
            savedAt.text = saved;
            battleTime.text = duration;
        }

        public void SetSelected(bool selected) => selectionFrame.SetActive(selected);
        private void Select() => _selected(_index);
        private void Remove() => _removed(_index);
        private void OnDestroy()
        {
            button.onClick.RemoveListener(Select);
            removeButton.onClick.RemoveListener(Remove);
        }
    }
}
