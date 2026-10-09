using CrazyLabs.Gameplay.Config;
using UnityEngine;

namespace CrazyLabs.Gameplay.Feedback
{
    public class FeedbackModule
    {
        private readonly AudioModule _audio;
        private readonly ParticleModule _particles;


        public FeedbackModule(AudioSource effects, AudioSource slideLoop, Transform player, ParticleSystem speedLines, AudioTuningData audioTuning, EffectsTuningData effectsTuning)
        {
            _audio = new(effects, slideLoop, audioTuning);
            _particles = new(player, speedLines, effectsTuning);
        }

        public void Tick(bool sliding, float speedNormalized)
        {
            _audio.Tick(sliding, speedNormalized);
            _particles.Tick(sliding, speedNormalized);
        }

        public void Dispose()
        {
            _audio.Dispose();
            _particles.Dispose();
        }
    }
}
