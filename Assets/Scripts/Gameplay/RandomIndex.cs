using UnityEngine;

namespace CrazyLabs.Gameplay
{
    public static class RandomIndex
    {
        public static int Different(int count, int last)
        {
            if (count <= 1)
            {
                return 0;
            }

            int index = Random.Range(0, count - 1);
            return index >= last ? index + 1 : index;
        }
    }
}
