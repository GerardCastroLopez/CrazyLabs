using UnityEngine;

namespace CrazyLabs.Gameplay.Controls
{
    public class TouchSource : IPointerSource
    {
        public PointerSample Poll()
        {
            if (Input.touchCount == 0)
            {
                return default;
            }

            var touch = Input.GetTouch(0);
            bool ended = touch.phase is TouchPhase.Ended or TouchPhase.Canceled;

            return new PointerSample {
                Active = true,
                Down = touch.phase == TouchPhase.Began,
                Held = !ended,
                Up = ended,
                Position = touch.position,
            };
        }
    }
}
