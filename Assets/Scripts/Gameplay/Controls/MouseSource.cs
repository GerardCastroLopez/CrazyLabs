using UnityEngine;

namespace CrazyLabs.Gameplay.Controls
{
    public class MouseSource : IPointerSource
    {
        public PointerSample Poll()
        {
            bool down = Input.GetMouseButtonDown(0);
            bool held = Input.GetMouseButton(0);
            bool up = Input.GetMouseButtonUp(0);

            return new PointerSample {
                Active = down || held || up,
                Down = down,
                Held = held,
                Up = up,
                Position = Input.mousePosition,
            };
        }
    }
}
