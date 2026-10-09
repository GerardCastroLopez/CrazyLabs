using CrazyLabs.Characters;
using CrazyLabs.Gameplay.Sled;
using Cysharp.Threading.Tasks;
using gSDK.MVC;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Gameplay.Player
{
    public class PlayerView : BaseView<PlayerController>
    {
        private static readonly int kIdle = Animator.StringToHash("Idle");
        private static readonly int kLaunch = Animator.StringToHash("Launch");
        private static readonly int kCrash = Animator.StringToHash("Crash");
        private static readonly int kVictory = Animator.StringToHash("Victory");

        [SerializeField] private Transform _visualsRoot;
        [SerializeField] private float _maxLeanDegrees = 22f;
        [Tooltip("How quickly the body follows the target lean.")]
        [SerializeField] private float _leanSpeed = 8f;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private AnimationClip _launchPlaceholder, _crashPlaceholder, _victoryPlaceholder;

        private GameObject _visualsInstance;
        private Animator _animator;
        private AnimatorOverrideController _overrides;
        private CharacterData _loadedCharacter;
        private int _lastLaunch = -1, _lastCrash = -1, _lastVictory = -1;
        private Quaternion _roll = Quaternion.identity;
        private Vector3 _visualsBasePosition;
        private bool _hasBasePosition;
        private float _bobPhase, _bobWeight, _bumpOffset, _bumpVelocity;


        protected override UniTask InternalShow(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InternalHide(bool animate)
        {
            return UniTask.CompletedTask;
        }

        protected override void Shown()
        {
            base.Shown();
            PlayIdle();
        }

        protected override void Destroy()
        {
            ReleaseVisuals();
            base.Destroy();
        }

        internal async UniTask LoadVisuals(CharacterData character)
        {
            if (_loadedCharacter == character && _visualsInstance)
            {
                return;
            }

            ReleaseVisuals();

            if (!character.VisualsPrefab.RuntimeKeyIsValid())
            {
                Debug.LogError($"Character '{character.Name}' has no visuals prefab assigned", this);
                return;
            }

            _visualsInstance = await character.VisualsPrefab.InstantiateAsync(_visualsRoot).Task.AsUniTask();
            _loadedCharacter = character;

            WireAnimator();
        }

        internal void Tick(float deltaTime)
        {
            var sled = _controller.Sled;
            transform.SetPositionAndRotation(sled.Position, sled.Rotation);

            if (_visualsRoot)
            {
                TickRide(sled, deltaTime);
            }
        }

        internal void Bump(float kick)
        {
            _bumpVelocity += kick * Mathf.Sqrt(_controller.Config.RideFeel.SpringStiffness);
        }

        private void TickRide(SledModule sled, float deltaTime)
        {
            var tuning = _controller.Config.RideFeel;

            if (!_hasBasePosition)
            {
                _visualsBasePosition = _visualsRoot.localPosition;
                _hasBasePosition = true;
            }

            var leanTarget = Quaternion.Euler(0f, 0f, -sled.TurnNormalized * _maxLeanDegrees);
            _roll = Quaternion.Slerp(_roll, leanTarget, deltaTime * _leanSpeed);

            float speed = sled.SpeedNormalized;
            _bobWeight = Mathf.MoveTowards(_bobWeight, sled.IsSliding ? speed : 0f, tuning.BobFadeSpeed * deltaTime);
            _bobPhase += Mathf.Lerp(tuning.BobFrequency.x, tuning.BobFrequency.y, speed) * deltaTime;

            float bob = Noise(_bobPhase, 0f) * tuning.BobAmplitude * _bobWeight;
            float wobble = Noise(_bobPhase * 0.8f, 1f) * tuning.PitchWobbleDegrees * _bobWeight;

            float acceleration = -tuning.SpringStiffness * _bumpOffset - tuning.SpringDamping * _bumpVelocity;
            _bumpVelocity += acceleration * deltaTime;
            _bumpOffset += _bumpVelocity * deltaTime;

            _visualsRoot.localPosition = _visualsBasePosition + Vector3.up * (bob + _bumpOffset);
            _visualsRoot.localRotation = _roll * Quaternion.Euler(wobble - _bumpOffset * tuning.BumpPitchPerMeter, 0f, 0f);
        }

        private static float Noise(float time, float channel)
        {
            return Mathf.PerlinNoise(time, channel * 7.3f) * 2f - 1f;
        }

        internal void PlayIdle()
        {
            if (!_animator || !_animator.isActiveAndEnabled)
            {
                return;
            }

            _bumpOffset = _bumpVelocity = 0f;
            ResetTriggers();
            _animator.Play(kIdle, 0, 0f);
            _animator.Update(0f);
        }

        internal void PlayLaunch()
        {
            SwapClip(_launchPlaceholder, _controller.Character.LaunchClips, ref _lastLaunch);
            PlayAnimation(kLaunch);
        }

        internal void PlayCrash()
        {
            SwapClip(_crashPlaceholder, _controller.Character.CrashClips, ref _lastCrash);
            PlayAnimation(kCrash);
        }

        internal void PlayVictory()
        {
            SwapClip(_victoryPlaceholder, _controller.Character.VictoryClips, ref _lastVictory);
            PlayAnimation(kVictory);
        }

        private void OnTriggerEnter(Collider other)
        {
            _controller.OnTrigger(other);
        }

        private void WireAnimator()
        {
            _animator = _visualsInstance.GetOrAddComponent<Animator>();

            if (!_animator.avatar)
            {
                Debug.LogError($"The Animator of '{_loadedCharacter.Name}' has no Avatar, so humanoid animations can't play", _animator);
            }

            _overrides = new AnimatorOverrideController(_animatorController);
            _animator.runtimeAnimatorController = _overrides;
            _animator.applyRootMotion = false;
        }

        private void ReleaseVisuals()
        {
            if (_visualsInstance)
            {
                Addressables.ReleaseInstance(_visualsInstance);
            }

            _visualsInstance = null;
            _animator = null;
            _overrides = null;
            _loadedCharacter = null;
            _lastLaunch = _lastCrash = _lastVictory = -1;
        }

        private void SwapClip(AnimationClip placeholder, AnimationClip[] pool, ref int last)
        {
            if (!_overrides || !placeholder || pool == null || pool.Length == 0)
            {
                return;
            }

            last = RandomIndex.Different(pool.Length, last);
            _overrides[placeholder] = pool[last];
        }

        private void PlayAnimation(int trigger)
        {
            if (!_animator || !_animator.isActiveAndEnabled)
            {
                return;
            }

            ResetTriggers();
            _animator.SetTrigger(trigger);
        }

        private void ResetTriggers()
        {
            _animator.ResetTrigger(kIdle);
            _animator.ResetTrigger(kLaunch);
            _animator.ResetTrigger(kCrash);
            _animator.ResetTrigger(kVictory);
        }
    }
}
