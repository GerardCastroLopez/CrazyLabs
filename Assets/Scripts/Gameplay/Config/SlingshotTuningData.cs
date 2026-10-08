using System;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class SlingshotTuningData
    {
        public float StartZ = 5f;
        public float MaxPullbackMeters = 2.5f;
        public float MinimumPullToFire = 0.12f;
        public float PostForwardOffset = 1.2f;
        public float PouchHeight = 0.9f;
    }
}
