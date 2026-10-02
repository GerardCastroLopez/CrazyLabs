using CrazyLabs.Core;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>Shakes the camera on collisions: a small shake for a slowdown, a strong one on a crash.</summary>
    public sealed class CameraShakeFeedback : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] FollowCamera followCamera;
        [SerializeField, Range(0f, 1f)] float slowHitShake = 0.35f;
        [SerializeField, Range(0f, 1f)] float crashShake = 1f;

        void OnEnable() => flow.ObstacleHit += OnObstacleHit;

        void OnDisable() => flow.ObstacleHit -= OnObstacleHit;

        void OnObstacleHit(Obstacle obstacle) =>
            followCamera.Shake(obstacle.Kind == ObstacleKind.Crash ? crashShake : slowHitShake);
    }
}
