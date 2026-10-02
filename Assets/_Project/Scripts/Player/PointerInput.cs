using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>
    /// Single-pointer abstraction over touchscreen and mouse. The first touch wins on devices with a
    /// touchscreen; otherwise the left mouse button is used. Reads Unity's legacy Input each call.
    /// </summary>
    public static class PointerInput
    {
        static bool HasTouch => Input.touchCount > 0;

        /// <summary>True on the frame the pointer goes down.</summary>
        public static bool Pressed => HasTouch ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);

        /// <summary>True while the pointer is down.</summary>
        public static bool Held
        {
            get
            {
                if (!HasTouch) return Input.GetMouseButton(0);
                var phase = Input.GetTouch(0).phase;
                return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
            }
        }

        /// <summary>Screen position of the pointer in pixels.</summary>
        public static Vector2 Position => HasTouch ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    }
}
