using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public interface IBuildPipeline
    {
        void OnFinishPipeline(string id, bool isSuccess, int countLeft);
    }
    [Serializable]
    public class BuildPipelineView: IBuildPipeline
    {
        public event Action<bool, string> OnFinishSequence; 
        [SerializeField] private Canvas canvas;

        [SerializeField] private List<PipelineView> pipelineViews;

        private Dictionary<string, PipelineView> _workingPipelines = new Dictionary<string, PipelineView>();

        public void Init()
        {
            foreach (PipelineView pipelineView in pipelineViews)
            {
                pipelineView.Init(this);
                pipelineView.Activate(false);
            }
        }
        
        public float AddPipeline(string id, Sprite icon, float fillTime)
        {
            canvas.enabled = true;
            if (_workingPipelines.TryGetValue(id, out PipelineView pipelineView))
            {
                pipelineView.AddCount();
                return pipelineView.TimeLeft;
            }

            PipelineView freePipeline = pipelineViews
                .FirstOrDefault(x => !x.IsBusy);
                
            SetUpPipeline(freePipeline);
            _workingPipelines.Add(id, freePipeline);
            return fillTime;

            void SetUpPipeline(PipelineView view)
            {
                view.SetIcon(icon);
                view.Activate(true);
                view.Fill(fillTime, id);
            }
        }

        private void OnComplete(string id, bool isSuccess)
        {
            OnFinishSequence?.Invoke(isSuccess, id);
            _workingPipelines.Remove(id);
        }

        public void OnFinishPipeline(string id, bool isSuccess, int countLeft)
        {
            OnFinishSequence?.Invoke(isSuccess, id);
            if (countLeft == 1)
            {
                _workingPipelines.Remove(id);
            }
        }
    }
}