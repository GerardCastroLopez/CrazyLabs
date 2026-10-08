using System;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class SledTuningData
    {
        public float MinLaunchSpeed = 9f;
        public float MaxLaunchSpeed = 17f;

        public float Gravity = 9.81f;
        public float Friction = 0.06f;
        public float AirDrag = 0.01f;
        public float MaxSpeed = 30f;
        public float MaxSpeedUpgradeFactor = 0.5f;

        public float TurnAcceleration = 200f;
        public float TurnDamping = 4f;
        public float MaxHeadingDegrees = 55f;
        public float CarveDrag = 0.05f;

        public float StallSpeed = 1.5f;
        public float StallGraceSeconds = 1.5f;
        public float StopDeceleration = 22f;
    }
}
