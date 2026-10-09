using CrazyLabs.Gameplay.Config;
using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class SlingshotModule
    {
        private readonly SlingshotTuningData _tuning;
        private readonly ControlsTuningData _controls;

        private float _keyboardCharge;

        public float Pull { get; private set; }
        public float Aim { get; private set; }
        public bool IsPulling => Pull > 0f;
        public float PullbackMeters => Pull * _tuning.MaxPullbackMeters;
        public float PouchSideMeters => -Aim * _tuning.MaxAimSideMeters;
        public float AimHeadingRadians => Aim * _tuning.MaxAimHeadingDegrees * Mathf.Deg2Rad;


        public SlingshotModule(SlingshotTuningData tuning, ControlsTuningData controls)
        {
            _tuning = tuning;
            _controls = controls;
        }

        public void Reset()
        {
            Pull = 0f;
            Aim = 0f;
            _keyboardCharge = 0f;
        }

        public void SetPull(float pull01)
        {
            Pull = Mathf.Clamp01(pull01);
            _keyboardCharge = Pull;
        }

        public void SetAim(float aim01)
        {
            Aim = Mathf.Clamp(aim01, -1f, 1f);
        }

        public void Charge(float deltaTime)
        {
            _keyboardCharge = Mathf.Clamp01(_keyboardCharge + deltaTime / _controls.KeyboardChargeSeconds);
            Pull = _keyboardCharge;
        }

        public bool Release(out float strength)
        {
            strength = Pull;
            Reset();
            return strength >= _tuning.MinimumPullToFire;
        }
    }
}