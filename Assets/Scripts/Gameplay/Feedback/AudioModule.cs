using CrazyLabs.Characters;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Events;
using gSDK.EventSystem;
using UnityEngine;

namespace CrazyLabs.Gameplay.Feedback
{
    public class AudioModule : IEventHandler<RunLaunchedEvent>, IEventHandler<CollectibleCollectedEvent>, IEventHandler<ObstacleHitEvent>, IEventHandler<RunEndedEvent>
    {
        private readonly AudioSource _effects, _slideLoop;
        private readonly AudioTuningData _tuning;

        private int _lastFemaleVoice = -1;
        private int _lastMaleVoice = -1;
        private float _slideAllowedAt;


        public AudioModule(AudioSource effects, AudioSource slideLoop, AudioTuningData tuning)
        {
            _effects = effects;
            _slideLoop = slideLoop;
            _tuning = tuning;

            _slideLoop.clip = tuning.SlideLoop;
            _slideLoop.loop = true;
            _slideLoop.playOnAwake = false;
            _slideLoop.volume = 0f;

            EventDispatcher.Register(this);
        }

        public void Tick(bool sliding, float speedNormalized)
        {
            bool canSlide = sliding && Time.time >= _slideAllowedAt;

            if (canSlide && !_slideLoop.isPlaying)
            {
                _slideLoop.Play();
            }
            else if (!canSlide && _slideLoop.isPlaying)
            {
                _slideLoop.Stop();
            }

            _slideLoop.volume = speedNormalized * _tuning.SlideLoopMaxVolume;
        }

        public void Handle(RunLaunchedEvent evt)
        {
            Play(_tuning.Launch);
            _slideAllowedAt = Time.time + Mathf.Max(_tuning.Launch ? _tuning.Launch.length : 0f, evt.AnimationSeconds);
        }

        public void Handle(CollectibleCollectedEvent evt)
        {
            Play(_tuning.Pickup);
        }

        public void Handle(ObstacleHitEvent evt)
        {
            foreach (var surface in _tuning.Surfaces)
            {
                if (surface.Surface == evt.Obstacle.Surface && surface.Clips is { Length: > 0 })
                {
                    Play(surface.Clips[Random.Range(0, surface.Clips.Length)]);
                    return;
                }
            }
        }

        public void Handle(RunEndedEvent evt)
        {
            if (evt.Result.IsWin)
            {
                Play(_tuning.Finish);
                return;
            }

            if (evt.Character.Voice == CharacterVoice.Female)
            {
                PlayVoice(_tuning.FemaleVoices, ref _lastFemaleVoice);
            }
            else
            {
                PlayVoice(_tuning.MaleVoices, ref _lastMaleVoice);
            }
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
        }

        private void PlayVoice(AudioClip[] voices, ref int last)
        {
            if (voices == null || voices.Length == 0)
            {
                return;
            }

            last = RandomIndex.Different(voices.Length, last);
            Play(voices[last]);
        }

        private void Play(AudioClip clip)
        {
            if (clip)
            {
                _effects.PlayOneShot(clip);
            }
        }
    }
}
