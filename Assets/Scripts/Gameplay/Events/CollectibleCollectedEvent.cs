using UnityEngine;

namespace CrazyLabs.Gameplay.Events
{
    public class CollectibleCollectedEvent
    {
        public readonly int Value;
        public readonly Vector3 Position;


        public CollectibleCollectedEvent(int value, Vector3 position)
        {
            Value = value;
            Position = position;
        }
    }
}
