using NUnit.Framework;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Tests
{
    public class TrackProfileTests
    {
        TrackLayout layout;

        [SetUp]
        public void SetUp()
        {
            layout = ScriptableObject.CreateInstance<TrackLayout>();
            layout.finishRunout = 20f;
            layout.sections = new[] { new TrackLayout.Section(100, 15), new TrackLayout.Section(100, 5) };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(layout);

        [Test]
        public void TrackDescendsMonotonically()
        {
            var profile = new TrackProfile(layout);
            float previous = profile.HeightAt(0f);
            for (float z = 5f; z < profile.FinishZ; z += 5f)
            {
                float height = profile.HeightAt(z);
                Assert.LessOrEqual(height, previous + 1e-4f);
                previous = height;
            }
        }

        [Test]
        public void RunoutIsFlat()
        {
            var profile = new TrackProfile(layout);
            Assert.AreEqual(0f, profile.PitchAt(profile.Length - 2f), 1e-3f);
        }

        [Test]
        public void LengthIncludesRunout()
        {
            var profile = new TrackProfile(layout);
            Assert.AreEqual(220f, profile.Length, 1e-3f);
            Assert.AreEqual(200f, profile.FinishZ, 1e-3f);
        }
    }
}
