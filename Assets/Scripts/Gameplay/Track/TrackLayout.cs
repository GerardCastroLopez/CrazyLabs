using System;

namespace CrazyLabs.Gameplay.Track
{
    [Serializable]
    public class TrackLayout
    {
        [Serializable]
        public struct Section
        {
            public float Length;
            public float PitchDegrees;
        }

        public float Width = 14f;
        public float FinishRunout = 30f;
        public Section[] Sections;

        public float TotalLength
        {
            get
            {
                float total = 0f;
                if (Sections != null)
                {
                    foreach (var section in Sections)
                    {
                        total += section.Length;
                    }
                }
                return total + FinishRunout;
            }
        }
    }
}
