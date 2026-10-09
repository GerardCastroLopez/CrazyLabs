using System;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class PickupFeedbackTuningData
    {
        public float HudPunchScale = 0.3f;
        public float HudPunchDuration = 0.25f;
        public float TextRiseDistance = 140f;
        public float TextDuration = 0.8f;
        public float TextStartScale = 0.6f;
        public int MaxActiveTexts = 8;
    }
}
