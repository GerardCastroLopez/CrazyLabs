using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class SpeedLinesTuningData
    {
        [Range(0f, 1f)] public float MinSpeedNormalized = 0.45f;
        public float MaxRate = 90f;
    }
}
