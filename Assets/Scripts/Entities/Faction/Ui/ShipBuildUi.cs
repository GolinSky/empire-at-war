using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public interface IShipBuildPresenter
    {
        void CompleteBuilding(bool isSuccess, string id);
    }

    public interface IShipBuildUi
    {
        void SetPresenter(IShipBuildPresenter presenter);
        void Initialize();
        void Dispose();
        void AddPipeline(UnitRequest unitRequest);
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

            pipelineView.Init();
            pipelineView.OnFinishSequence += _presenter.CompleteBuilding;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            pipelineView.OnFinishSequence -= _presenter.CompleteBuilding;
            _isInitialized = false;
        }

        public void AddPipeline(UnitRequest unitRequest)
        {
            if (unitRequest == null)
            {
                throw new ArgumentNullException(nameof(unitRequest));
            }

            pipelineView.AddPipeline(
                unitRequest.Id,
                unitRequest.FactionData.Icon,
                unitRequest.FactionData.BuildTime);
        }

    }
}
