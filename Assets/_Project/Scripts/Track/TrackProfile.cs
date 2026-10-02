using UnityEngine;

namespace CrazyLabs.Track
{
    /// <summary>
    /// Sampled height/pitch of the slope along the Z axis. The track runs along +Z and descends,
    /// so everything that needs "the ground" (sled, props, ground mesh) queries this one object.
    /// </summary>
    public sealed class TrackProfile
    {
        const float SampleStep = 1f;
        const int SmoothingRadius = 8;

        readonly float[] heights;
        readonly float[] pitches; // radians, positive = downhill

        public float Length { get; }
        public float HalfWidth { get; }
        public float FinishZ { get; }

        public TrackProfile(TrackLayout layout)
        {
            Length = layout.TotalLength;
            HalfWidth = layout.width * 0.5f;
            FinishZ = Length - layout.finishRunout;

            int count = Mathf.CeilToInt(Length / SampleStep) + 2;
            pitches = new float[count];
            heights = new float[count];

            FillRawPitches(layout);
            Smooth(pitches);
            Smooth(pitches);

            for (int i = 1; i < count; i++)
                heights[i] = heights[i - 1] - Mathf.Tan(pitches[i - 1]) * SampleStep;
        }

        public float HeightAt(float z) => Sample(heights, z);

        public float PitchAt(float z) => Sample(pitches, z);

        float Sample(float[] values, float z)
        {
            float index = Mathf.Clamp(z / SampleStep, 0f, values.Length - 1.001f);
            int lower = (int)index;
            return Mathf.Lerp(values[lower], values[lower + 1], index - lower);
        }

        void FillRawPitches(TrackLayout layout)
        {
            int sectionIndex = 0;
            float sectionEnd = layout.sections.Length > 0 ? layout.sections[0].length : 0f;

            for (int i = 0; i < pitches.Length; i++)
            {
                float z = i * SampleStep;
                while (sectionIndex < layout.sections.Length - 1 && z >= sectionEnd)
                {
                    sectionIndex++;
                    sectionEnd += layout.sections[sectionIndex].length;
                }

                bool pastSlope = z >= Length - layout.finishRunout || layout.sections.Length == 0;
                pitches[i] = pastSlope ? 0f : layout.sections[sectionIndex].pitchDegrees * Mathf.Deg2Rad;
            }
        }

        /// <summary>Box blur so section boundaries become gentle transitions instead of kinks.</summary>
        static void Smooth(float[] values)
        {
            var source = (float[])values.Clone();
            for (int i = 0; i < values.Length; i++)
            {
                float sum = 0f;
                int taps = 0;
                for (int k = -SmoothingRadius; k <= SmoothingRadius; k++)
                {
                    int j = Mathf.Clamp(i + k, 0, values.Length - 1);
                    sum += source[j];
                    taps++;
                }
                values[i] = sum / taps;
            }
        }
    }
}
