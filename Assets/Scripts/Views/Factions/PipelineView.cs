using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class PipelineView:MonoBehaviour
    {
        private const float FillImageStartValue = 1f;
        
        [SerializeField] private Image fillIcon;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;

        private Sequence fillImageSequence;
        private int count = 1;
        private float fillTime;
        private float tweenStartTime;
        private Action  onComplete;
        public bool IsBusy { get; private set; }
        public float TimeLeft => (tweenStartTime - Time.time) + (count-1)*fillTime;


        public void Fill(float fillTime, Action onComplete)
        {
            this.onComplete = onComplete;
            this.fillTime = fillTime;
            if (fillImageSequence != null && fillImageSequence.IsActive())
            {
                return;
            }
            fillImageSequence = DOTween.Sequence();

            FillImage();
        }

        private void FillImage()
        {
            tweenStartTime = Time.time + fillTime;
            fillIcon.fillAmount = FillImageStartValue;
            fillImageSequence.Append(fillIcon.DOFillAmount(0, fillTime)
                .OnComplete(Complete));
        }

        private void Complete()
        {
            if (count == 1)
            {
                Activate(false);
                onComplete?.Invoke();
            }
            else
            {
                count--;
                countText.text = count.ToString();
                
                FillImage();
            }
        }

        public void AddCount()
        {
            count++;
            countText.text = count.ToString();
        }
        
        public void SetIcon(Sprite icon)
        {
            this.icon.sprite = icon;
           

            countText.text = count.ToString();
        }

        public void Activate(bool isActive)
        {
            gameObject.SetActive(isActive);
            IsBusy = isActive;
        }
    }
}