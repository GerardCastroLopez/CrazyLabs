using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>
    /// Produces a steering value in [-1, 1]. Touchscreen or mouse: drag horizontally relative to where
    /// the press started (see <see cref="PointerInput"/>). Keyboard: A/D or arrow keys.
    /// </summary>
    public sealed class SteeringInput : MonoBehaviour
    {
        [Tooltip("Horizontal drag, as a fraction of screen width, that gives full steering.")]
        [SerializeField, Range(0.02f, 0.5f)] float fullDragFraction = 0.12f;

        float pressX;

        public float Steer { get; private set; }

        void Update()
        {
            if (PointerInput.Pressed) pressX = PointerInput.Position.x;

            if (PointerInput.Held)
            {
                float range = Screen.width * fullDragFraction;
                Steer = Mathf.Clamp((PointerInput.Position.x - pressX) / range, -1f, 1f);
            }
            else
            {
                Steer = Input.GetAxisRaw("Horizontal");
            }
        }
    }
}
