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
        void SetPresenter(IShipBuildPresenter presenter);
        void Initialize();
        void Dispose();
        void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots);
        void SetParent(Transform parent);
        void Show();
        void Hide();
    }

    public class ShipBuildUi : BaseUi, IShipBuildUi
    {
        [SerializeField] private BuildPipelineView pipelineView;

        private IShipBuildPresenter _presenter;
        private bool _isInitialized;

        public void SetPresenter(IShipBuildPresenter presenter)
        {
            _presenter = presenter ??
                throw new ArgumentNullException(nameof(presenter));
        }

        public void Initialize()
        {
            if (_presenter == null)
            {
                throw new InvalidOperationException(
                    "Ship build presenter must be set before initialization.");
            }

            if (pipelineView == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(pipelineView)} is not assigned.");
            }

            if (_isInitialized)
            {
                return;
            }

            pipelineView.Init(_presenter.CancelBuilding);
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

        public void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "Ship build UI must be initialized before rendering pipelines.");
            }

            pipelineView.Render(snapshots);
        }

    }
}
