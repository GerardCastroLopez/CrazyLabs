using UnityEngine;

namespace CrazyLabs.Gameplay.Track
{
    public class Obstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleKind _kind = ObstacleKind.Crash;
        [SerializeField] private ObstacleSurface _surface = ObstacleSurface.Stone;
        [Tooltip("Fraction of speed kept after a Slow hit.")]
        [SerializeField, Range(0.1f, 1f)] private float _speedKept = 0.55f;

        public ObstacleKind Kind => _kind;
        public ObstacleSurface Surface => _surface;
        public float SpeedKept => _speedKept;

        public void ResetState()
        {
            if (TryGetComponent(out Collider hitbox))
            {
                hitbox.enabled = true;
            }
        }

        public void Consume()
        {
            if (TryGetComponent(out Collider hitbox))
            {
                hitbox.enabled = false;
            }
        }
    }
}
