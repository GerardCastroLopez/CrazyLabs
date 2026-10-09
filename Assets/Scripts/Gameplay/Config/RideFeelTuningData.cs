using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class RideFeelTuningData
    {
        public float BobAmplitude = 0.05f;
        public Vector2 BobFrequency = new(3f, 9f);
        public float PitchWobbleDegrees = 1.5f;
        public float BobFadeSpeed = 6f;

        public float SlowBumpKick = 0.18f;
        public float CrashBumpKick = 0.4f;
        public float LaunchKick = 0.22f;
        public float BumpPitchPerMeter = 25f;
        public float SpringStiffness = 120f;
        public float SpringDamping = 12f;
    }
}
