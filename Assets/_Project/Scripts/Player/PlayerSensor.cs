using System;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>Translates trigger overlaps into gameplay events. Contains no game rules itself.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerSensor : MonoBehaviour
    {
        public event Action<Collectible> CollectibleTouched;
        public event Action<Obstacle> ObstacleTouched;
        public event Action FinishTouched;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Collectible collectible)) CollectibleTouched?.Invoke(collectible);
            else if (other.TryGetComponent(out Obstacle obstacle)) ObstacleTouched?.Invoke(obstacle);
            else if (other.TryGetComponent(out FinishLine _)) FinishTouched?.Invoke();
        }
    }
}
