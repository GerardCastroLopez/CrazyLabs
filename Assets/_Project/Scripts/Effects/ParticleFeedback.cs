using System.Collections;
using CrazyLabs.Core;
using CrazyLabs.Player;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Effects
{
    /// <summary>
    /// Plays particle effects in response to game flow events: croissant pickups, obstacle hits,
    /// the course-complete celebration, and a speed-driven dust trail while sliding.
    /// One-shot effects are instantiated and destroyed after <see cref="effectLifetime"/> seconds.
    /// </summary>
    public sealed class ParticleFeedback : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] SledMotor sled;

        [Header("One-shot effects")]
        [SerializeField] GameObject pickupEffect;
        [SerializeField] GameObject softHitEffect;
        [SerializeField] GameObject crashEffect;
        [SerializeField] float effectLifetime = 6f;

        [Header("Course completed")]
        [SerializeField] GameObject fireworkEffect;
        [SerializeField] GameObject confettiEffect;
        [SerializeField] float fireworkInterval = 0.3f;
        [SerializeField] Vector3[] fireworkOffsets =
        {
            new Vector3(-4f, 5f, 6f), new Vector3(4f, 6f, 10f), new Vector3(0f, 8f, 14f), new Vector3(-2f, 6f, 18f),
        };

        [Header("Slide trail (child of the player)")]
        [SerializeField] ParticleSystem slideTrail;
        [Tooltip("Emission multiplier at standstill and at top speed.")]
        [SerializeField] Vector2 trailIntensity = new Vector2(0.25f, 1.6f);

        float trailBaseRate;

        void Awake()
        {
            if (slideTrail == null) return;

            // Dust must stay behind the sled instead of travelling with it.
            var main = slideTrail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            trailBaseRate = slideTrail.emission.rateOverTimeMultiplier;
            slideTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnEnable()
        {
            flow.CollectibleCollected += OnCollected;
            flow.ObstacleHit += OnObstacleHit;
            flow.RunEnded += OnRunEnded;
        }

        void OnDisable()
        {
            flow.CollectibleCollected -= OnCollected;
            flow.ObstacleHit -= OnObstacleHit;
            flow.RunEnded -= OnRunEnded;
        }

        void Update() => UpdateSlideTrail();

        void UpdateSlideTrail()
        {
            if (slideTrail == null) return;

            if (!sled.IsSliding)
            {
                if (slideTrail.isPlaying) slideTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (!slideTrail.isPlaying) slideTrail.Play();
            var emission = slideTrail.emission;
            emission.rateOverTimeMultiplier = trailBaseRate * Mathf.Lerp(trailIntensity.x, trailIntensity.y, sled.SpeedNormalized);
        }

        void OnCollected(int value, Vector3 position) => Spawn(pickupEffect, position);

        void OnObstacleHit(Obstacle obstacle)
        {
            var effect = obstacle.Kind == ObstacleKind.Crash ? crashEffect : softHitEffect;
            Spawn(effect, obstacle.transform.position + Vector3.up);
        }

        void OnRunEnded(RunResult result)
        {
            if (result.IsWin) StartCoroutine(Celebrate(sled.transform.position));
        }

        IEnumerator Celebrate(Vector3 origin)
        {
            Spawn(confettiEffect, origin + Vector3.up * 7f);

            var wait = new WaitForSeconds(fireworkInterval);
            foreach (var offset in fireworkOffsets)
            {
                Spawn(fireworkEffect, origin + offset);
                yield return wait;
            }
        }

        void Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            Destroy(Instantiate(prefab, position, Quaternion.identity), effectLifetime);
        }
    }
}
