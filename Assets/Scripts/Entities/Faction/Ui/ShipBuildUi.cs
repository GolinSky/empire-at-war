using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public interface IShipBuildPresenter
    {
        void CancelBuilding(string id);
    }

    public interface IShipBuildUi
    {
        void Initialize();

        void Dispose();

        void SetPresenter(IShipBuildPresenter presenter);

        void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots);

        void SetParent(Transform parent);

        void Show();

        void Hide();
    }

    public class ShipBuildUi : BaseUi, IShipBuildUi, ITooltipHoverView
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

            _isInitialized = false;
        }

        public void SetPresenter(IShipBuildPresenter presenter)
        {
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
