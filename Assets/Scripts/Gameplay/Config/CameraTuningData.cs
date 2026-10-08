using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class CameraTuningData
    {
        public Vector3 Offset = new(0f, 4.2f, -8.5f);
        public Vector3 LookAhead = new(0f, 0.8f, 7f);
        public float FollowSmoothTime = 0.18f;
        public float LateralFollowFactor = 0.5f;
        public float BaseFov = 55f;
        public float MaxFovBoost = 14f;
        public float FovLerpSpeed = 3f;

        public float MaxShakeOffset = 0.6f;
        public float MaxShakeRollDegrees = 4f;
        public float ShakeFrequency = 28f;
        public float TraumaDecay = 1.4f;
        public float SlowHitShake = 0.35f;
        public float CrashShake = 1f;
    }
}
