using System;
using gSDK.MVC;
using gSDK.UI;

namespace CrazyLabs.Gameplay.UI
{
    public class PauseController : PopupViewController
    {
        private readonly Action _onResume;
        private readonly Action _onRestart;
        private readonly Action _onForfeit;


        public PauseController(Action onResume, Action onRestart, Action onForfeit, PopupService popupService) : base("UI/Prefabs/PausePopup", 1, false, popupService)
        {
            _onResume = onResume;
            _onRestart = onRestart;
            _onForfeit = onForfeit;
        }

        public override void ClosePopup()
        {
            base.ClosePopup();
            ViewState.AddListener(OnClosed, true);
        }

        internal void Restart()
        {
            _onRestart?.Invoke();
        }

        internal void Forfeit()
        {
            _onForfeit?.Invoke();
        }

        private void OnClosed(eViewState state)
        {
            if (state.Is(eViewState.Hidden))
            {
                ViewState.RemoveListener(OnClosed);
                _onResume?.Invoke();
            }
        }
    }
}
