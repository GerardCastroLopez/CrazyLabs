using System;
using UnityEngine;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class SpawnTuningData
    {
        public GameObject CollectiblePrefab;

        public float FirstSpawnZ = 55f;
        [Tooltip("No props are placed within this distance of the finish line.")]
        public float PropsStopBeforeFinish = 30f;
        public Vector2 SpawnSpacing = new(11f, 19f);
        [Tooltip("Props stay this far from the track edge.")]
        public float LaneEdgeMargin = 1.5f;
        [Tooltip("Props get a random yaw in [-range, +range] degrees.")]
        public float PropYawRange = 25f;
        [Tooltip("Min and max sideways gap between two obstacles in the same row, so there is always a way through.")]
        public Vector2 PairGap = new(5f, 8f);

        [Tooltip("Chance that a collectible row wiggles instead of running straight.")]
        [Range(0f, 1f)] public float SwayChance = 0.5f;
        public Vector2 SwayAmount = new(0.6f, 1.4f);
        public float SwayFrequency = 0.9f;
        public int CollectiblesPerRow = 6;
        public float CollectibleSpacing = 2.4f;
        public float CollectibleHeight = 0.9f;

        public float SceneryDensity = 9f;
        [Tooltip("Multiplies the scenery spacing by a random value in this range.")]
        public Vector2 ScenerySpacingJitter = new(0.6f, 1.4f);
        [Tooltip("Scenery starts this far from the track edge...")]
        public float SceneryMinDistance = 2f;
        [Tooltip("...and is spread up to this much further out.")]
        public float SceneryExtraDistance = 14f;
        public Vector2 SceneryScaleRange = new(0.8f, 1.5f);
    }
}
