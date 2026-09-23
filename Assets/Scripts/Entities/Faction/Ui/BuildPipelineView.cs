using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    [Serializable]
    public class BuildPipelineView
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private List<PipelineView> pipelineViews;

        private Dictionary<string, PipelineView> _workingPipelines = new Dictionary<string, PipelineView>();

        public void Init(Action<string> cancelBuilding)
        {
            foreach (PipelineView pipelineView in pipelineViews)
            {
                pipelineView.Init(cancelBuilding);
                pipelineView.Activate(false);
            }
        }
        
        public void Render(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
            }

            RemoveMissingPipelines(snapshots);
            foreach (ProductionQueueSnapshot snapshot in snapshots)
            {
                if (!_workingPipelines.TryGetValue(
                        snapshot.UnitRequest.Id,
                        out PipelineView pipelineView))
                {
                    pipelineView = pipelineViews.FirstOrDefault(view => !view.IsBusy)
                        ?? throw new InvalidOperationException(
                            "Production snapshot exceeds the configured pipeline view capacity.");
                    _workingPipelines.Add(snapshot.UnitRequest.Id, pipelineView);
                }

                pipelineView.Render(snapshot);
                pipelineView.Activate(true);
            }
        }

        private void RemoveMissingPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            HashSet<string> activeIds = new HashSet<string>(
                snapshots.Select(snapshot => snapshot.UnitRequest.Id));
            List<string> completedIds = _workingPipelines.Keys
                .Where(id => !activeIds.Contains(id))
                .ToList();

            foreach (string completedId in completedIds)
            {
                PipelineView pipelineView = _workingPipelines[completedId];
                pipelineView.Activate(false);
                _workingPipelines.Remove(completedId);
            }
        }
    }
}
