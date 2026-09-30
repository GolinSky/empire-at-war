using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    [Serializable]
    public class BuildPipelineView
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private List<PipelineView> pipelineViews;

        private Dictionary<UnitLimitKey, PipelineView> _workingPipelines = new Dictionary<UnitLimitKey, PipelineView>();

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
                UnitLimitKey key = UnitLimitKey.From(snapshot.UnitRequest);
                if (!_workingPipelines.TryGetValue(key, out PipelineView pipelineView))
                {
                    pipelineView = pipelineViews.FirstOrDefault(view => !view.IsBusy)
                        ?? throw new InvalidOperationException(
                            "Production snapshot exceeds the configured pipeline view capacity.");
                    _workingPipelines.Add(key, pipelineView);
                }

                pipelineView.Render(snapshot);
                pipelineView.Activate(true);
            }
        }

        private void RemoveMissingPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            HashSet<UnitLimitKey> activeIds = new HashSet<UnitLimitKey>(
                snapshots.Select(snapshot => UnitLimitKey.From(snapshot.UnitRequest)));
            List<UnitLimitKey> completedIds = _workingPipelines.Keys
                .Where(id => !activeIds.Contains(id))
                .ToList();

            foreach (UnitLimitKey completedId in completedIds)
            {
                PipelineView pipelineView = _workingPipelines[completedId];
                pipelineView.Activate(false);
                _workingPipelines.Remove(completedId);
            }
        }
    }
}
