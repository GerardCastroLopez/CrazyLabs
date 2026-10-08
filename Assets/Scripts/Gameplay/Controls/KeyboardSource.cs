using UnityEngine;

namespace CrazyLabs.Gameplay.Controls
{
    public class KeyboardSource
    {
        private const string kHorizontalAxis = "Horizontal";

        private readonly KeyCode _actionKey;

        public float Horizontal { get; private set; }
        public bool ActionDown { get; private set; }
        public bool ActionHeld { get; private set; }
        public bool ActionUp { get; private set; }
        public bool IsActive => Mathf.Abs(Horizontal) > 0f || ActionDown || ActionHeld || ActionUp;


        public KeyboardSource(KeyCode actionKey)
        {
            _actionKey = actionKey;
        }

        public void Tick()
        {
            Horizontal = Input.GetAxisRaw(kHorizontalAxis);
            ActionDown = Input.GetKeyDown(_actionKey);
            ActionHeld = Input.GetKey(_actionKey);
            ActionUp = Input.GetKeyUp(_actionKey);
        }
    }
}
