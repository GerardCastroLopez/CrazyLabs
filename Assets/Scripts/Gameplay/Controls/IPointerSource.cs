using UnityEngine;

namespace CrazyLabs.Gameplay.Controls
{
    public struct PointerSample
    {
        public bool Active;
        public bool Down, Held, Up;
        public Vector2 Position;
    }

    public interface IPointerSource
    {
        PointerSample Poll();
    }
}
