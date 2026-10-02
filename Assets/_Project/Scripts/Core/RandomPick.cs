using UnityEngine;

namespace CrazyLabs.Core
{
    /// <summary>Random selection that avoids repeating the previous choice.</summary>
    public static class RandomPick
    {
        /// <param name="count">Number of options.</param>
        /// <param name="last">Index chosen last time, or -1.</param>
        public static int Index(int count, int last)
        {
            if (count <= 1) return 0;
            int index = Random.Range(0, count - 1);
            return index >= last ? index + 1 : index; // skips 'last' without bias
        }
    }
}
