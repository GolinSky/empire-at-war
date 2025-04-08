using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.Views.Factions
{
    public class PipelineView:MonoBehaviour
    {
        private const float FILL_IMAGE_START_VALUE = 1f;
        
        [SerializeField] private Image fillIcon;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button skipButton;
        
        private IBuildPipeline _buildPipeline;
        private Sequence _fillImageSequence;
        private float _fillTime;
        private float _tweenStartTime;
        private int _count = 1;
        public bool IsBusy { get; private set; }
        public float TimeLeft => (_tweenStartTime - Time.time) + (_count-1)*_fillTime;
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
            _fillImageSequence.KillIfExist();
            DOTween.Kill(fillIcon);
            Complete(false);
        }

        public void Fill(float fillTime, string id)
        {
            Id = id;
            _fillTime = fillTime;
            _fillImageSequence.KillIfExist();
            _fillImageSequence = DOTween.Sequence();
            FillImage();
        }

        private void FillImage()
        {
            _tweenStartTime = Time.time + _fillTime;
            fillIcon.fillAmount = FILL_IMAGE_START_VALUE;
            _fillImageSequence.Append(fillIcon.DOFillAmount(0, _fillTime)
                .OnComplete(Complete));
        }

        private void Complete()
        {
            Complete(true);
        }

        private void Complete(bool isSuccess)
        {
            if (_count == 1)
            {
                Activate(false);
            }
            else
            {
                _count--;
                countText.text = _count.ToString();
                FillImage();
            }
            _buildPipeline.OnFinishPipeline(Id, isSuccess, _count);
        }

        public void AddCount()
        {
            _count++;
            countText.text = _count.ToString();
        }
        
        public void SetIcon(Sprite icon)
        {
            this.icon.sprite = icon;
            countText.text = _count.ToString();
        }

        public void Activate(bool isActive)
        {
            gameObject.SetActive(isActive);
            IsBusy = isActive;
        }

        public void Init(IBuildPipeline buildPipeline)
        {
            _buildPipeline = buildPipeline;
        }
    }
}