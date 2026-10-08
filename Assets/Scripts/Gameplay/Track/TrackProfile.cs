using UnityEngine;

namespace CrazyLabs.Gameplay.Track
{
    public class TrackProfile
    {
        private const float kSampleStep = 1f;
        private const int kSmoothingRadius = 8;
        private const float kIndexEpsilon = 0.001f;

        private readonly float[] _heights;
        private readonly float[] _pitches;

        public float Length { get; }
        public float HalfWidth { get; }
        public float FinishZ { get; }


        public TrackProfile(TrackLayout layout)
        {
            Length = layout.TotalLength;
            HalfWidth = layout.Width * 0.5f;
            FinishZ = Length - layout.FinishRunout;

            int count = Mathf.CeilToInt(Length / kSampleStep) + 2;
            _pitches = new float[count];
            _heights = new float[count];

            FillRawPitches(layout);
            Smooth(_pitches);
            Smooth(_pitches);

            for (int i = 1; i < count; i++)
            {
                _heights[i] = _heights[i - 1] - Mathf.Tan(_pitches[i - 1]) * kSampleStep;
            }
        }

        public float HeightAt(float z)
        {
            return Sample(_heights, z);
        }

        public float PitchAt(float z)
        {
            return Sample(_pitches, z);
        }

        private static float Sample(float[] values, float z)
        {
            float index = Mathf.Clamp(z / kSampleStep, 0f, values.Length - 1f - kIndexEpsilon);
            int lower = (int)index;
            return Mathf.Lerp(values[lower], values[lower + 1], index - lower);
        }

        private void FillRawPitches(TrackLayout layout)
        {
            var sections = layout.Sections;
            int sectionIndex = 0;
            float sectionEnd = sections is { Length: > 0 } ? sections[0].Length : 0f;

            for (int i = 0; i < _pitches.Length; i++)
            {
                float z = i * kSampleStep;
                while (sectionIndex < sections.Length - 1 && z >= sectionEnd)
                {
                    sectionIndex++;
                    sectionEnd += sections[sectionIndex].Length;
                }

                bool pastSlope = z >= Length - layout.FinishRunout || sections.Length == 0;
                _pitches[i] = pastSlope ? 0f : sections[sectionIndex].PitchDegrees * Mathf.Deg2Rad;
            }
        }

        private static void Smooth(float[] values)
        {
            var source = (float[])values.Clone();
            for (int i = 0; i < values.Length; i++)
            {
                float sum = 0f;
                int taps = 0;
                for (int k = -kSmoothingRadius; k <= kSmoothingRadius; k++)
                {
                    sum += source[Mathf.Clamp(i + k, 0, values.Length - 1)];
                    taps++;
                }
                values[i] = sum / taps;
            }
        }
    }
}
