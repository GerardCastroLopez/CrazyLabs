using System;
using UnityEngine;

namespace CrazyLabs.Track
{
    /// <summary>Design-time description of the slope: a sequence of sections with different pitch.</summary>
    [CreateAssetMenu(menuName = "CrazyLabs/Track Layout", fileName = "TrackLayout")]
    public sealed class TrackLayout : ScriptableObject
    {
        [Serializable]
        public struct Section
        {
            [Min(1f)] public float length;
            [Tooltip("Downhill angle in degrees. Near zero sections bleed momentum.")]
            public float pitchDegrees;

            public Section(float length, float pitchDegrees)
            {
                this.length = length;
                this.pitchDegrees = pitchDegrees;
            }
        }

        public float width = 14f;
        [Tooltip("Flat runout after the finish line, in meters.")]
        public float finishRunout = 30f;
        public Section[] sections =
        {
            new Section(90, 16),
            new Section(70, 8),
            new Section(45, 1.5f),
            new Section(110, 14),
            new Section(70, 5),
            new Section(45, 0.5f),
            new Section(130, 16),
            new Section(90, 9),
            new Section(100, 13),
        };

        public float TotalLength
        {
            get
            {
                float total = 0f;
                foreach (var section in sections) total += section.length;
                return total + finishRunout;
            }
        }
    }
}
