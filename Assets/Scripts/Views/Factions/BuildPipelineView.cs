using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    [Serializable]
    public class BuildPipelineView
    {
        [SerializeField] private Canvas canvas;

        [SerializeField] private List<PipelineView> pipelineViews;

        private Dictionary<string, PipelineView> workingPipelines = new Dictionary<string, PipelineView>();

        public void Init()
        {
            foreach (PipelineView pipelineView in pipelineViews)
            {
                pipelineView.Activate(false);
            }
        }
        
        public float AddPipeline(string id, Sprite icon, float fillTime)
        {
            canvas.enabled = true;
            if (workingPipelines.TryGetValue(id, out PipelineView pipelineView))
            {
                pipelineView.AddCount();
                AddPipelineInternal(pipelineView);
                return pipelineView.TimeLeft;
            }
            else
            {
                PipelineView freePipeline = pipelineViews
                    .Where(x => !x.IsBusy)
                    .FirstOrDefault();
                
                AddPipelineInternal(freePipeline);
                workingPipelines.Add(id, freePipeline);
                return fillTime;
            }

            void AddPipelineInternal(PipelineView view)
            {
                view.SetIcon(icon);
                view.Activate(true);
                view.Fill(fillTime, ()=>OnComplete(id));
            }
        }

        private void OnComplete(string id)
        {
            workingPipelines.Remove(id);
        }
    }
}