using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>Base (un-upgraded) physics and handling values for the sled. Edit the asset to tune feel.</summary>
    [CreateAssetMenu(menuName = "CrazyLabs/Sled Tuning", fileName = "SledTuning")]
    public sealed class SledTuning : ScriptableObject
    {
        [Header("Launch (m/s, scaled by slingshot pull)")]
        public float minLaunchSpeed = 9f;
        public float maxLaunchSpeed = 17f;

        [Header("Slope physics")]
        public float gravity = 9.81f;
        [Tooltip("Rolling friction coefficient. Flat sections bleed speed through this and drag.")]
        public float friction = 0.06f;
        public float airDrag = 0.01f;
        public float maxSpeed = 30f;

        [Header("Steering (heading control)")]
        [Tooltip("Turning acceleration while steering, in degrees/s^2. Steering rotates the sled's heading; nothing straightens it again.")]
        public float turnAcceleration = 200f;
        [Tooltip("Resistance to spinning (1/s). Stops the turn rate once you release the input; it does not recentre the heading.")]
        public float turnDamping = 4f;
        [Tooltip("How far across the slope the sled can point, in degrees from straight downhill.")]
        public float maxHeadingDegrees = 55f;
        [Tooltip("Speed scrubbed off while turning (m/s^2 per rad/s of turn, per m/s of speed).")]
        public float carveDrag = 0.05f;

        [Header("Run end")]
        [Tooltip("Below this speed (after the grace period) the run ends for lack of momentum.")]
        public float stallSpeed = 1.5f;
        public float stallGraceSeconds = 1.5f;
        public float stopDeceleration = 22f;
    }
}
