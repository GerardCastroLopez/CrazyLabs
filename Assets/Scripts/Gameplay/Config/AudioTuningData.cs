using System;
using CrazyLabs.Gameplay.Track;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class AudioTuningData
    {
        public AudioClip Launch, Pickup, Finish, SlideLoop;
        public SurfaceSound[] Surfaces;
        public AudioClip[] FemaleVoices, MaleVoices;
        public float SlideLoopMaxVolume = 1f;

        [Serializable]
        public struct SurfaceSound
        {
            public ObstacleSurface Surface;
            public AudioClip[] Clips;
        }
    }
}
