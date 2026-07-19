using System;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Selection.Marquee
{
    public interface IMarqueeSelectionPresenter
    {
        event Action<MarqueeRectangle> Completed;
    }

    public sealed class MarqueeSelectionPresenter : IMarqueeSelectionPresenter, IInitializable, ILateDisposable
    {
        private readonly IInputService _inputService;
        private readonly MarqueeSelectionModel _model;
        private readonly IMarqueeSelectionView _view;

        public event Action<MarqueeRectangle> Completed;

        public MarqueeSelectionPresenter(
            IInputService inputService,
            MarqueeSelectionModel model,
            IMarqueeSelectionView view)
        {
            _inputService = inputService;
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _inputService.OnPrimaryDragStarted += HandleDragStarted;
            _inputService.OnPrimaryDragChanged += HandleDragChanged;
            _inputService.OnPrimaryDragEnded += HandleDragEnded;
        }

        public void LateDispose()
        {
            _inputService.OnPrimaryDragStarted -= HandleDragStarted;
            _inputService.OnPrimaryDragChanged -= HandleDragChanged;
            _inputService.OnPrimaryDragEnded -= HandleDragEnded;
            _model.Cancel();
            _view.Hide();
        }

        private void HandleDragStarted(Vector2 screenPosition)
        {
            _model.Begin(ToPoint(screenPosition));
            _view.Show(_model.Rectangle);
        }

        private void HandleDragChanged(Vector2 screenPosition)
        {
            _model.Update(ToPoint(screenPosition));
            _view.Show(_model.Rectangle);
        }

        private void HandleDragEnded(Vector2 screenPosition)
        {
            if (!_model.IsActive)
            {
                return;
            }

            MarqueeRectangle rectangle = _model.Complete(ToPoint(screenPosition));
            _view.Hide();
            Completed?.Invoke(rectangle);
        }

        private static MarqueePoint ToPoint(Vector2 screenPosition)
        {
            return new MarqueePoint(screenPosition.x, screenPosition.y);
        }
    }
}
