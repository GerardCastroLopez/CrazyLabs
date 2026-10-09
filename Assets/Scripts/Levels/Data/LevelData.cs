using System;
using CrazyLabs.Gameplay.Track;
using UnityEngine;

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
        public GameObject[] CrashObstacles, SlowObstacles, Scenery;
        public GameObject AmbientEffect;
    }
}