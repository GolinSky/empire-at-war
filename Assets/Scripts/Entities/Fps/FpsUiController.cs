using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Fps
{
    public sealed class FpsUiController : UiController, IFpsPresenter, IInitializable, ITickable, ILateDisposable
    {
        private readonly FpsModel _model;
        private IFpsUi _ui;

        public FpsUiController(IUiService uiService, IUiCancelRouter cancelRouter, FpsModel model)
            : base(uiService, cancelRouter)
        {
            _model = model;
        }

        public void Initialize()
        {
            _ui = (IFpsUi)UiService.CreateUi(UiType.Fps);
            _ui.SetModel(_model);
            _ui.Initialize();
        }

        public void Tick()
        {
            SampleFrame(Time.unscaledDeltaTime);
        }

        public void SampleFrame(float unscaledDeltaTime)
        {
            _model.SampleFrame(unscaledDeltaTime);
        }

        public void LateDispose()
        {
            _ui.Dispose();
        }
    }
}
