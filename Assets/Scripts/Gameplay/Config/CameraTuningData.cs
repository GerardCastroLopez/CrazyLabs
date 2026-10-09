using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class CameraTuningData
    {
        public Vector3 Offset = new(0f, 3.2f, -6.5f);
        public Vector3 LookAhead = new(0f, 0.8f, 7f);
        public float FollowSmoothTime = 0.18f;
        public float LateralFollowFactor = 0.5f;
        [Range(0f, 1f)] public float PitchFollowFactor = 1f;
        public float BaseFov = 55f;
        public float MaxFovBoost = 14f;
        public float FovLerpSpeed = 3f;
        public float LaunchFovKick = 8f;
        public float LaunchKickDecay = 32f;
        public float LaunchShake = 0.25f;

        public float MaxShakeOffset = 0.6f;
        public float MaxShakeRollDegrees = 4f;
        public float ShakeFrequency = 28f;
        public float TraumaDecay = 1.4f;
        public float SlowHitShake = 0.35f;
        public float CrashShake = 1f;
    }
}
