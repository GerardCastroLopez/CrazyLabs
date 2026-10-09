using CrazyLabs.Gameplay.Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using gSDK.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CrazyLabs.Audio
{
    public class MusicService : BaseService
    {
        private const int kSourceCount = 2;

        private readonly AudioSource[] _sources = new AudioSource[kSourceCount];
        private readonly AsyncOperationHandle<AudioClip>[] _handles = new AsyncOperationHandle<AudioClip>[kSourceCount];
        private AudioTuningData _tuning;
        private string _requestedKey, _playingKey;
        private int _active = -1;
        private int _request;


        public MusicService() : base(false)
        {
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();

            _tuning = _locator.GetConfig<GameplayConfigSO>().Audio;

            var root = new GameObject("Music");
            Object.DontDestroyOnLoad(root);

            for (int i = 0; i < kSourceCount; i++)
            {
                var source = root.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.ignoreListenerPause = true;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        public void PlayMenu()
        {
            Play(_tuning.MenuMusic);
        }

        public void Play(AssetReferenceT<AudioClip> reference)
        {
            PlayAsync(reference).Forget();
        }

        private async UniTaskVoid PlayAsync(AssetReferenceT<AudioClip> reference)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                return;
            }

            string key = reference.RuntimeKey.ToString();

            if (key == _requestedKey)
            {
                return;
            }

            _requestedKey = key;
            int request = ++_request;

            var handle = Addressables.LoadAssetAsync<AudioClip>(reference.RuntimeKey);
            AudioClip clip;

            try
            {
                clip = await handle.Task.AsUniTask();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"MusicService: couldn't load '{key}'. {exception.Message}");
                Release(handle);

                if (request == _request)
                {
                    _requestedKey = _playingKey;
                }

                return;
            }

            if (request != _request)
            {
                Release(handle);
                return;
            }

            CrossFade(clip, handle, key);
        }

        private void CrossFade(AudioClip clip, AsyncOperationHandle<AudioClip> handle, string key)
        {
            int previousIndex = _active;
            _active = (_active + 1) % kSourceCount;
            _playingKey = key;

            var next = _sources[_active];
            next.DOKill();
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            next.DOFade(_tuning.MusicVolume, _tuning.MusicFadeSeconds).SetUpdate(true);
            _handles[_active] = handle;

            if (previousIndex >= 0)
            {
                var previous = _sources[previousIndex];
                previous.DOKill();
                previous.DOFade(0f, _tuning.MusicFadeSeconds).SetUpdate(true).OnComplete(() => Stop(previousIndex));
            }
        }

        private void Stop(int index)
        {
            if (index == _active)
            {
                return;
            }

            _sources[index].Stop();
            _sources[index].clip = null;
            Release(_handles[index]);
            _handles[index] = default;
        }

        private static void Release(AsyncOperationHandle<AudioClip> handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }
}
