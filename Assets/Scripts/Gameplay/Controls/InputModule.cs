using CrazyLabs.Gameplay.Config;
using gSDK;
using UnityEngine;

namespace CrazyLabs.Gameplay.Controls
{
    public class InputModule
    {
        private readonly IPointerSource[] _pointerSources;
        private readonly KeyboardSource _keyboard;
        private readonly ControlsTuningData _tuning;

        private Vector2 _lastPosition;
        private float _maxDistanceFromPress;
        private bool _pressBlocked;

        public bool PointerDown { get; private set; }
        public bool PointerHeld { get; private set; }
        public bool PointerUp { get; private set; }
        public bool PointerTap { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public Vector2 PressPosition { get; private set; }
        public Vector2 PointerDelta { get; private set; }
        public float HoldTime { get; private set; }
        public Vector2 DragFromPress => PointerHeld || PointerUp ? PointerPosition - PressPosition : Vector2.zero;
        public Vector2 DragFromPressNormalized => new(DragFromPress.x / Screen.width, DragFromPress.y / Screen.height);

        public float Horizontal => _keyboard.Horizontal;
        public bool ActionDown => _keyboard.ActionDown;
        public bool ActionHeld => _keyboard.ActionHeld;
        public bool ActionUp => _keyboard.ActionUp;

        public readonly Reactive<eInputDevice> ActiveDevice;


        public InputModule(ControlsTuningData tuning, Reactive<eInputDevice> activeDevice)
        {
            _tuning = tuning;
            ActiveDevice = activeDevice;
            _pointerSources = new IPointerSource[] { new TouchSource(), new MouseSource() };
            _keyboard = new KeyboardSource(tuning.ActionKey);
        }

        public void Tick(float deltaTime)
        {
            _keyboard.Tick();
            TickPointer(PollPointer(), deltaTime);

            if (PointerDown || PointerHeld || PointerUp)
            {
                ActiveDevice.Value = eInputDevice.Pointer;
            }
            else if (_keyboard.IsActive)
            {
                ActiveDevice.Value = eInputDevice.Keyboard;
            }
        }

        private PointerSample PollPointer()
        {
            foreach (var source in _pointerSources)
            {
                var sample = source.Poll();
                if (sample.Active)
                {
                    return sample;
                }
            }
            return default;
        }

        private void TickPointer(PointerSample sample, float deltaTime)
        {
            PointerDown = sample.Down;
            PointerHeld = sample.Held;
            PointerUp = sample.Up;
            PointerTap = false;
            PointerDelta = Vector2.zero;

            if (sample.Active && sample.Down && sample.OverUI)
            {
                _pressBlocked = true;
            }

            if (_pressBlocked)
            {
                PointerDown = PointerHeld = PointerUp = false;
                _pressBlocked = sample.Active && !sample.Up;
                return;
            }

            if (!sample.Active)
            {
                return;
            }

            PointerPosition = sample.Position;

            if (sample.Down)
            {
                PressPosition = sample.Position;
                HoldTime = 0f;
                _maxDistanceFromPress = 0f;
            }
            else
            {
                PointerDelta = sample.Position - _lastPosition;
                HoldTime += deltaTime;
                _maxDistanceFromPress = Mathf.Max(_maxDistanceFromPress, (sample.Position - PressPosition).magnitude);
            }
            _lastPosition = sample.Position;

            if (sample.Up)
            {
                PointerTap = HoldTime <= _tuning.TapMaxSeconds && _maxDistanceFromPress <= _tuning.TapMaxMovePixels;

            }
        }
    }
}
