using System;
using EmpireAtWar.Services.Input;
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
        private readonly IPointerGestures _gestures;
        private readonly IMarqueeSelectionView _view;

        private readonly MarqueeSelectionModel _model;

        public event Action<MarqueeRectangle> Completed;

        public MarqueeSelectionPresenter(
            IPointerGestures gestures,
            IMarqueeSelectionView view,
            MarqueeSelectionModel model)
        {
            _gestures = gestures;
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _gestures.DragStarted += HandleDragStarted;
            _gestures.DragChanged += HandleDragChanged;
            _gestures.DragEnded += HandleDragEnded;
        }

        public void LateDispose()
        {
            _gestures.DragStarted -= HandleDragStarted;
            _gestures.DragChanged -= HandleDragChanged;
            _gestures.DragEnded -= HandleDragEnded;
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
