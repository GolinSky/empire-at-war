using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Fps
{
    public sealed class FpsUiController : UiController, IInitializable, ITickable, ILateDisposable
    {
        private FpsUi _ui;

        private readonly FpsModel _model;

        public FpsUiController(IUiService uiService, IUiCancelRouter cancelRouter, FpsModel model)
            : base(uiService, cancelRouter)
        {
            _model = model;
        }

        public void Initialize()
        {
            _ui = CreateUi<FpsUi>();
            _ui.SetModel(_model);
            _ui.Initialize();
            _ui.Show();
        }

        public void LateDispose()
        {
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }

        public void Tick()
        {
            SampleFrame(Time.unscaledDeltaTime);
        }

        public void SampleFrame(float unscaledDeltaTime)
        {
            _model.SampleFrame(unscaledDeltaTime);
        }
    }
}
