using System;
using UnityEngine;

namespace gSDK
{
    [AttributeUsage(AttributeTargets.Field)]
    public class rRangeAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public rRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class rTextAreaAttribute : PropertyAttribute
    {
        public int minLines;
        public int maxLines;

        public rTextAreaAttribute(int minLines = 3, int maxLines = 3)
        {
            this.minLines = minLines;
            this.maxLines = maxLines;
        }
    }

}