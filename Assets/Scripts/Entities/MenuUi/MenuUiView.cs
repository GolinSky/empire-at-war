using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace EmpireAtWar.Entities.MenuUi
{
    public interface IMenuUiView
    {
        void SetModel(IMenuUiModel model);
        void SetPresenter(IMenuUiPresenter presenter);
        void Initialize();
        void Dispose();
    }

    public class MenuUiView : BaseUi, IMenuUiView
    {
        [SerializeField] private Button startDemoButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitApplicationButton;

        private IMenuUiModel _model;
        private IMenuUiPresenter _presenter;
        private bool _isInitialized;

        public void SetModel(IMenuUiModel model)
        {
            _model = model;
        }

        public void SetPresenter(IMenuUiPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Initialize()
        {
            if (_presenter == null)
            {
                throw new InvalidOperationException("MenuUiView dependencies must be set before initialization.");
            }

            startDemoButton.onClick.AddListener(_presenter.StartDemo);
            optionsButton.onClick.AddListener(_presenter.OpenOptions);
            quitApplicationButton.onClick.AddListener(_presenter.ExitApplication);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            startDemoButton.onClick.RemoveListener(_presenter.StartDemo);
            optionsButton.onClick.RemoveListener(_presenter.OpenOptions);
            quitApplicationButton.onClick.RemoveListener(_presenter.ExitApplication);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}