using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class GroundTuningData
    {
        public float ExtraBehind = 150f;
        public float ExtraAhead = 150f;
        public float TrackStripStep = 2f;
        public float SurroundStripStep = 6f;
        public float SurroundHalfWidth = 90f;
        [Tooltip("The surround sits slightly below the track so the two never z-fight.")]
        public float SurroundYOffset = -0.06f;
        [Tooltip("Meters of ground covered by one repeat of the ground texture.")]
        public float UvTileSize = 6f;
    }
}
