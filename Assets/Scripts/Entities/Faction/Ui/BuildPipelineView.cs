using EmpireAtWar.Components.Ui.Tooltip;
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
        [SerializeField] private PipelineView pipelinePrefab;
        [SerializeField] private Transform pipelineParent;
        private Action<string> _cancelBuilding;
        private TooltipHoverView _tooltipHover;

        private Dictionary<UnitLimitKey, PipelineView> _workingPipelines = new Dictionary<UnitLimitKey, PipelineView>();
        public void RegisterTooltips(TooltipHoverView hover)
        {
            _tooltipHover = hover;
            foreach (PipelineView view in pipelineViews) hover.Register(view.TooltipTrigger);
        }

        public void Init(Action<string> cancelBuilding)
        {
            _cancelBuilding = cancelBuilding;
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
                    pipelineView = pipelineViews.FirstOrDefault(view => !view.IsBusy);
                    if (pipelineView == null)
                    {
                        pipelineView = UnityEngine.Object.Instantiate(pipelinePrefab, pipelineParent);
                        pipelineView.Init(_cancelBuilding);
                        _tooltipHover.Register(pipelineView.TooltipTrigger);
                        pipelineViews.Add(pipelineView);
                    }
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
