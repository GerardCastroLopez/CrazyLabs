using CrazyLabs.Gameplay.Config;
using CrazyLabs.Upgrades;
using CrazyLabs.Upgrades.Data;
using UnityEngine;

namespace CrazyLabs.Gameplay.Sled
{
    public readonly struct SledStats
    {
        private const float kMinTurnDamping = 0.01f;

        public readonly float MinLaunchSpeed, MaxLaunchSpeed;
        public readonly float Gravity, Friction, AirDrag, MaxSpeed;
        public readonly float TurnAcceleration, TurnDamping, CarveDrag;
        public readonly float MaxHeading;
        public readonly float MaxTurnRate;
        public readonly float StallSpeed, StallGraceSeconds, StopDeceleration;
        public readonly float CoinMultiplier;


        public SledStats(SledTuningData tuning, UpgradesModule upgrades)
        {
            float launch = upgrades.GetBonus(UpgradeType.LaunchPower);
            float speed = upgrades.GetBonus(UpgradeType.Speed);
            float steering = upgrades.GetBonus(UpgradeType.Steering);
            float coins = upgrades.GetBonus(UpgradeType.CollectibleValue);

            MinLaunchSpeed = tuning.MinLaunchSpeed * (1f + launch);
            MaxLaunchSpeed = tuning.MaxLaunchSpeed * (1f + launch);

            Gravity = tuning.Gravity;
            Friction = tuning.Friction / (1f + speed);
            AirDrag = tuning.AirDrag / (1f + speed);
            MaxSpeed = tuning.MaxSpeed * (1f + speed * tuning.MaxSpeedUpgradeFactor);

            TurnAcceleration = tuning.TurnAcceleration * Mathf.Deg2Rad * (1f + steering);
            TurnDamping = tuning.TurnDamping;
            CarveDrag = tuning.CarveDrag;
            MaxHeading = tuning.MaxHeadingDegrees * Mathf.Deg2Rad;
            MaxTurnRate = TurnAcceleration / Mathf.Max(kMinTurnDamping, TurnDamping);

            StallSpeed = tuning.StallSpeed;
            StallGraceSeconds = tuning.StallGraceSeconds;
            StopDeceleration = tuning.StopDeceleration;

            CoinMultiplier = 1f + coins;
        }

        public float LaunchSpeedForPull(float pull01)
        {
            return Mathf.Lerp(MinLaunchSpeed, MaxLaunchSpeed, Mathf.Clamp01(pull01));
        }
    }
}
