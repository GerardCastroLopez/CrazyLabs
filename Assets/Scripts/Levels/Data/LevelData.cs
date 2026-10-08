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
        public GameObject[] CrashObstacles, SlowObstacles, Scenery;
        public GameObject AmbientEffect;
    }
}