using CrazyLabs.Progression;

namespace CrazyLabs.Player
{
    /// <summary>Tuning values with the player's upgrades applied. Recomputed before every run.</summary>
    public readonly struct SledStats
    {
        public readonly float MinLaunchSpeed;
        public readonly float MaxLaunchSpeed;
        public readonly float Gravity;
        public readonly float Friction;
        public readonly float AirDrag;
        public readonly float MaxSpeed;
        public readonly float TurnAcceleration;   // rad/s^2
        public readonly float TurnDamping;
        public readonly float MaxHeading;         // rad
        public readonly float CarveDrag;
        public readonly float MaxTurnRate;        // rad/s, the terminal turn rate under full input
        public readonly float StallSpeed;
        public readonly float StallGraceSeconds;
        public readonly float StopDeceleration;
        public readonly float CoinMultiplier;

        SledStats(SledTuning t, float launch, float speed, float steering, float coins)
        {
            MinLaunchSpeed = t.minLaunchSpeed * launch;
            MaxLaunchSpeed = t.maxLaunchSpeed * launch;
            Gravity = t.gravity;
            Friction = t.friction / (1f + speed);
            AirDrag = t.airDrag / (1f + speed);
            MaxSpeed = t.maxSpeed * (1f + speed * 0.5f);
            TurnAcceleration = t.turnAcceleration * UnityEngine.Mathf.Deg2Rad * (1f + steering);
            TurnDamping = t.turnDamping;
            MaxHeading = t.maxHeadingDegrees * UnityEngine.Mathf.Deg2Rad;
            CarveDrag = t.carveDrag;
            MaxTurnRate = TurnAcceleration / UnityEngine.Mathf.Max(0.01f, t.turnDamping);
            StallSpeed = t.stallSpeed;
            StallGraceSeconds = t.stallGraceSeconds;
            StopDeceleration = t.stopDeceleration;
            CoinMultiplier = 1f + coins;
        }

        public static SledStats Create(SledTuning tuning, ProgressionService progression) => new SledStats(
            tuning,
            progression.GetBonus(UpgradeType.LaunchPower),
            progression.GetBonus(UpgradeType.Speed),
            progression.GetBonus(UpgradeType.Steering),
            progression.GetBonus(UpgradeType.CollectibleValue));

        public float LaunchSpeedForPull(float pull01) =>
            UnityEngine.Mathf.Lerp(MinLaunchSpeed, MaxLaunchSpeed, UnityEngine.Mathf.Clamp01(pull01));
    }
}
