using System;
using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class ShipBuildUi : BaseUi
    {
        private IShipBuildPresenter _presenter;

        [SerializeField] private BuildPipelineView pipelineView;
        [SerializeField] private GameObject queuePanel;
        [SerializeField] private TooltipHoverView tooltipHover;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_presenter == null)
            {
                throw new InvalidOperationException(
                    "Ship build presenter must be set before initialization.");
            }

            if (_isInitialized)
            {
                return;
            }

            pipelineView.Init(_presenter.CancelBuilding);
            pipelineView.RegisterTooltips(tooltipHover);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            pipelineView.Dispose();
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();

        public void AssignPresenter(IShipBuildPresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("ShipBuildUi presenter can only be assigned once.");
            _presenter = presenter ??
                throw new ArgumentNullException(nameof(presenter));
        }

        public void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "Ship build UI must be initialized before rendering pipelines.");
            }

            pipelineView.Render(snapshots);
            queuePanel.SetActive(snapshots.Count > 0);
        }

    }
}
