using UnityEngine;

namespace CrazyLabs.Track
{
    /// <summary>Marks a trigger volume as an obstacle.</summary>
    public sealed class Obstacle : MonoBehaviour
    {
        [SerializeField] ObstacleKind kind = ObstacleKind.Crash;
        [SerializeField] ObstacleSurface surface = ObstacleSurface.Stone;
        [Tooltip("Fraction of speed kept after a Slow hit.")]
        [SerializeField, Range(0.1f, 1f)] float speedKept = 0.55f;

        public ObstacleKind Kind => kind;
        public ObstacleSurface Surface => surface;
        public float SpeedKept => speedKept;

        /// <summary>Prevents the same obstacle from hitting twice while the sled overlaps it.</summary>
        public void Consume()
        {
            if (TryGetComponent(out Collider hitbox)) hitbox.enabled = false;
        }
    }
}
