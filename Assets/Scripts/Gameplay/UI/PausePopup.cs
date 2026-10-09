using CrazyLabs.UI;

namespace CrazyLabs.Gameplay.UI
{
    public class PausePopup : PopupView<PauseController>
    {
        public void OnResumeTouch()
        {
            _controller.ClosePopup();
        }

        public void OnRestartTouch()
        {
            _controller.Restart();
        }

        public void OnForfeitTouch()
        {
            _controller.Forfeit();
        }
    }
}
