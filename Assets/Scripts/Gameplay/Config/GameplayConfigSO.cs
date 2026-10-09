using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [CreateAssetMenu]
    public class GameplayConfigSO : ScriptableObject
    {
        public SledTuningData Sled;
        public ControlsTuningData Controls;
        public SlingshotTuningData Slingshot;
        public CameraTuningData CameraRig;
        public AudioTuningData Audio;
        public EffectsTuningData Effects;
        public FlowTuningData Flow;
        public GroundTuningData Ground;
        public SpawnTuningData Spawn;
        public PickupFeedbackTuningData PickupFeedback;
        public RideFeelTuningData RideFeel;
    }
}
