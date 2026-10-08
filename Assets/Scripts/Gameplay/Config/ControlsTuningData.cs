using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class ControlsTuningData
    {
        public float HorizontalDragFullFraction = 0.12f;
        public float VerticalDragFullFraction = 0.2f;
        public float KeyboardChargeSeconds = 1.1f;
        public float TapMaxSeconds = 0.3f;
        public float TapMaxMovePixels = 24f;
        public KeyCode ActionKey = KeyCode.Space;
    }
}
