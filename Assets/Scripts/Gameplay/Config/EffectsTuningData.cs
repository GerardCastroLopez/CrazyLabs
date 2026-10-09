using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Gameplay.Config
{
    [Serializable]
    public class EffectsTuningData
    {
        public AssetReferenceGameObject PickupEffect, SoftHitEffect, CrashEffect, FireworkEffect, ConfettiEffect, SlideTrail;
        public float OneShotLifetime = 6f;
        public int InitialPoolSize = 2;
        public float HitEffectHeight = 1f;
        public Vector3[] FireworkOffsets = { new(-4f, 5f, 6f), new(4f, 6f, 10f), new(0f, 8f, 14f), new(-2f, 6f, 18f) };
        public float FireworkInterval = 0.3f;
        public float ConfettiHeight = 7f;
        public Vector3 TrailLocalPosition = new(0f, 0.15f, -0.6f);
        public Vector2 TrailIntensity = new(0.25f, 1.6f);
        public SpeedLinesTuningData SpeedLines = new();
    }
}
