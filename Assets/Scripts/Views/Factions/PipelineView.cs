using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.Views.Factions
{
    public class PipelineView:MonoBehaviour
    {
        private const float FillImageStartValue = 1f;
        
        [SerializeField] private Image fillIcon;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button skipButton;
        
        private IBuildPipeline buildPipeline;
        private Sequence fillImageSequence;
        private float fillTime;
        private float tweenStartTime;
        private int count = 1;
        public bool IsBusy { get; private set; }
        public float TimeLeft => (tweenStartTime - Time.time) + (count-1)*fillTime;
        private string Id { get; set; }

        private void Awake()
        {
            skipButton.onClick.AddListener(SkipSequence);
        }

        private void OnDestroy()
        {
            skipButton.onClick.RemoveListener(SkipSequence);
        }

        private void SkipSequence()
        {
            fillImageSequence.KillIfExist();
            Complete(false);
        }

        public void Fill(float fillTime, string id)
        {
            Id = id;
            this.fillTime = fillTime;
            fillImageSequence.KillIfExist();
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
            Complete(true);
        }

        private void Complete(bool isSuccess)
        {
            if (count == 1)
            {
                Activate(false);
            }
            else
            {
                count--;
                countText.text = count.ToString();
                FillImage();
            }
            buildPipeline.OnFinishPipeline(Id, isSuccess, count);
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

        public void Init(IBuildPipeline buildPipeline)
        {
            this.buildPipeline = buildPipeline;
        }
    }
}