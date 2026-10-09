using System;
using System.Collections.Generic;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Events;
using CrazyLabs.Gameplay.Track;
using Cysharp.Threading.Tasks;
using gSDK;
using gSDK.EventSystem;
using gSDK.Patterns.Pooling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace CrazyLabs.Gameplay.Feedback
{
    public class ParticleModule : IEventHandler<CollectibleCollectedEvent>, IEventHandler<ObstacleHitEvent>, IEventHandler<RunEndedEvent>
    {
        private readonly Transform _player;
        private readonly EffectsTuningData _tuning;
        private readonly Dictionary<string, AddressablePool<Transform>> _pools = new();
        private readonly HashSet<string> _failedPools = new();
        private readonly ParticleSystem _speedLines;

        private ParticleSystem[] _trail = Array.Empty<ParticleSystem>();
        private float[] _trailBaseRates = Array.Empty<float>();
        private GameObject _trailInstance;
        private bool _disposed;


        public ParticleModule(Transform player, ParticleSystem speedLines, EffectsTuningData tuning)
        {
            _player = player;
            _tuning = tuning;

            CreateTrail().Forget();
            _speedLines = speedLines;

            EventDispatcher.Register(this);
        }

        public void Tick(bool sliding, float speedNormalized)
        {
            TickSpeedLines(sliding, speedNormalized);

            for (int i = 0; i < _trail.Length; i++)
            {
                var system = _trail[i];

                if (!sliding)
                {
                    if (system.isEmitting)
                    {
                        system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                    continue;
                }

                if (!system.isPlaying)
                {
                    system.Play();
                }

                var emission = system.emission;
                emission.rateOverTimeMultiplier = _trailBaseRates[i] * Mathf.Lerp(_tuning.TrailIntensity.x, _tuning.TrailIntensity.y, speedNormalized);
            }
        }

        public void Handle(CollectibleCollectedEvent evt)
        {
            PlayOneShot(_tuning.PickupEffect, evt.Position).Forget();
        }

        public void Handle(ObstacleHitEvent evt)
        {
            var effect = evt.Obstacle.Kind == ObstacleKind.Crash ? _tuning.CrashEffect : _tuning.SoftHitEffect;
            PlayOneShot(effect, evt.Obstacle.transform.position + Vector3.up * _tuning.HitEffectHeight).Forget();
        }

        public void Handle(RunEndedEvent evt)
        {
            if (evt.Result.IsWin)
            {
                Celebrate(_player.position).Forget();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            EventDispatcher.Unregister(this);

            _pools.Values.Foreach(pool => pool.Dispose());
            _pools.Clear();

            AddressableInstance.ReleaseOrDestroy(_trailInstance);

            if (_speedLines)
            {
                _speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private async UniTaskVoid CreateTrail()
        {
            if (!IsValid(_tuning.SlideTrail))
            {
                return;
            }

            try
            {
                var instance = await AddressableInstance.Instantiate(_tuning.SlideTrail.RuntimeKey, _player);

                if (_disposed)
                {
                    AddressableInstance.ReleaseOrDestroy(instance);
                    return;
                }

                _trailInstance = instance;
                instance.transform.localPosition = _tuning.TrailLocalPosition;

                var systems = instance.GetComponentsInChildren<ParticleSystem>();
                var baseRates = new float[systems.Length];

                for (int i = 0; i < systems.Length; i++)
                {
                    var main = systems[i].main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    baseRates[i] = systems[i].emission.rateOverTimeMultiplier;
                    systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                _trailBaseRates = baseRates;
                _trail = systems;
            }
            catch (Exception exception)
            {
                Debug.LogError($"ParticleModule: couldn't load the slide trail. {exception.Message}");
            }
        }

        private void TickSpeedLines(bool sliding, float speedNormalized)
        {
            if (!_speedLines)
            {
                return;
            }

            var settings = _tuning.SpeedLines;
            float intensity = sliding ? Mathf.InverseLerp(settings.MinSpeedNormalized, 1f, speedNormalized) : 0f;

            if (intensity > 0f && !_speedLines.isPlaying)
            {
                _speedLines.Play();
            }

            var emission = _speedLines.emission;
            emission.rateOverTime = settings.MaxRate * intensity;
        }

        private async UniTaskVoid Celebrate(Vector3 origin)
        {
            PlayOneShot(_tuning.ConfettiEffect, origin + Vector3.up * _tuning.ConfettiHeight).Forget();

            foreach (var offset in _tuning.FireworkOffsets)
            {
                PlayOneShot(_tuning.FireworkEffect, origin + offset).Forget();
                await UniTask.Delay(TimeSpan.FromSeconds(_tuning.FireworkInterval));

                if (_disposed)
                {
                    return;
                }
            }
        }

        private async UniTaskVoid PlayOneShot(AssetReferenceGameObject reference, Vector3 position)
        {
            if (!IsValid(reference))
            {
                return;
            }

            string key = reference.RuntimeKey.ToString();

            if (_failedPools.Contains(key))
            {
                return;
            }

            var pool = GetPool(key, reference);
            Transform instance;

            try
            {
                instance = await pool.GetAsync();
            }
            catch (Exception exception)
            {
                _failedPools.Add(key);
                Debug.LogError($"ParticleModule: couldn't load '{key}', it will be skipped. {exception.Message}");
                return;
            }

            if (_disposed)
            {
                return;
            }

            instance.SetPositionAndRotation(position, Quaternion.identity);

            foreach (var system in instance.GetComponentsInChildren<ParticleSystem>())
            {
                system.Clear(true);
                system.Play(true);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_tuning.OneShotLifetime));

            if (!_disposed)
            {
                pool.Return(instance);
            }
        }

        private AddressablePool<Transform> GetPool(string key, AssetReferenceGameObject reference)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool;
            }

            pool = new AddressablePool<Transform>(key, reference, _tuning.InitialPoolSize);
            _pools.Add(key, pool);
            return pool;
        }

        private static bool IsValid(AssetReference reference)
        {
            return reference != null && reference.RuntimeKeyIsValid();
        }
    }
}
