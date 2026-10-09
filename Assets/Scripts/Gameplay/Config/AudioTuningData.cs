using System;
using CrazyLabs.Gameplay.Track;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class AudioTuningData
    {
        public AudioClip Launch, Pickup, Finish, SlideLoop;
        public SurfaceSound[] Surfaces;
        public AudioClip[] FemaleVoices, MaleVoices;
        public float SlideLoopMaxVolume = 1f;
        public AssetReferenceT<AudioClip> MenuMusic;
        [Range(0f, 1f)] public float MusicVolume = 0.6f;
        public float MusicFadeSeconds = 1f;

        [Serializable]
        public struct SurfaceSound
        {
            public ObstacleSurface Surface;
            public AudioClip[] Clips;
        }
    }
}
