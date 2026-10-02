using System;
using CrazyLabs.Core;
using CrazyLabs.Selection;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>Plays sound effects in response to game flow events.</summary>
    public sealed class AudioFeedback : MonoBehaviour
    {
        [Serializable]
        public struct SurfaceSound
        {
            public ObstacleSurface surface;
            public AudioClip[] clips;
        }

        [SerializeField] GameFlow flow;
        [SerializeField] SledMotor sled;
        [SerializeField] AudioSource effects;
        [SerializeField] AudioSource slideLoop;

        [Header("Gameplay")]
        [SerializeField] AudioClip launch;
        [SerializeField] AudioClip pickup;
        [SerializeField] AudioClip finish;
        [Tooltip("Impact sound per obstacle material; one clip is picked at random.")]
        [SerializeField] SurfaceSound[] surfaceSounds;
        [Tooltip("Reaction voices when a run is lost, per character voice type.")]
        [SerializeField] AudioClip[] femaleVoices;
        [SerializeField] AudioClip[] maleVoices;

        [Header("UI")]
        [SerializeField] AudioClip uiClick;
        [SerializeField] AudioClip upgradePurchased;

        int lastFemaleVoice = -1;
        int lastMaleVoice = -1;

        void OnEnable()
        {
            flow.Launched += OnLaunched;
            flow.CollectibleCollected += OnCollected;
            flow.ObstacleHit += OnObstacleHit;
            flow.RunEnded += OnRunEnded;
            flow.CharacterPicked += OnCharacterPicked;
            flow.Progression.Purchased += OnPurchased;
        }

        void OnDisable()
        {
            flow.Launched -= OnLaunched;
            flow.CollectibleCollected -= OnCollected;
            flow.ObstacleHit -= OnObstacleHit;
            flow.RunEnded -= OnRunEnded;
            flow.CharacterPicked -= OnCharacterPicked;
            flow.Progression.Purchased -= OnPurchased;
        }

        void Update()
        {
            if (slideLoop == null) return;
            bool sliding = sled.IsSliding;
            if (sliding && !slideLoop.isPlaying) slideLoop.Play();
            else if (!sliding && slideLoop.isPlaying) slideLoop.Stop();
            slideLoop.volume = sled.SpeedNormalized;
        }

        public void PlayUiClick() => Play(uiClick);

        void OnLaunched() => Play(launch);
        void OnCollected(int _, Vector3 __) => Play(pickup);
        void OnPurchased(Progression.UpgradeDefinition _) => Play(upgradePurchased);
        void OnCharacterPicked() => Play(flow.Character.SelectSound);

        void OnObstacleHit(Obstacle obstacle)
        {
            foreach (var entry in surfaceSounds)
            {
                if (entry.surface != obstacle.Surface || entry.clips == null || entry.clips.Length == 0) continue;
                Play(entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)]);
                return;
            }
        }

        void OnRunEnded(RunResult result)
        {
            if (result.IsWin) Play(finish);
            else PlayVoice(flow.Character.Voice);
        }

        void PlayVoice(CharacterVoice voice)
        {
            bool female = voice == CharacterVoice.Female;
            var pool = female ? femaleVoices : maleVoices;
            if (pool == null || pool.Length == 0) return;

            ref int last = ref (female ? ref lastFemaleVoice : ref lastMaleVoice);
            last = RandomPick.Index(pool.Length, last);
            Play(pool[last]);
        }

        void Play(AudioClip clip)
        {
            if (clip != null && effects != null) effects.PlayOneShot(clip);
        }
    }
}
