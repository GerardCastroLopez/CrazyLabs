using System;
using CrazyLabs.Gameplay.Track;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Levels.Data
{
    [Serializable]
    public class LevelData
    {
        public float TrackLength;
        public string DisplayName;
        public TrackLayout Layout;
        public Material TrackMaterial,  SurroundMaterial;
        public Color SkyColor, AmbientColor, SunColor;
        public float FogStart, FogEnd;
        [Range(0f, 1f)] public float CrashChance = 0.3f, SlowChance = 0.25f, PairChance = 0.35f;
        public AssetReferenceGameObject[] CrashObstacles, SlowObstacles, Scenery;
        public AssetReferenceGameObject AmbientEffect;
        public AssetReferenceT<AudioClip> Music;
    }
}