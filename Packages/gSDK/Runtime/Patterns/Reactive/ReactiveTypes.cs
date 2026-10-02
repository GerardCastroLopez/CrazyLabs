using System;
using UnityEngine;

namespace gSDK
{
    [Serializable]
    public class rBool : Reactive<bool>
    {
        public rBool() : this(default) { }
        public rBool(bool value) : base(value) { }
    }

    [Serializable]
    public class rInt : Reactive<int>
    {
        public rInt() : this(default) { }
        public rInt(int value) : base(value) { }
    }

    [Serializable]
    public class rUint : Reactive<uint>
    {
        public rUint() : this(default) { }
        public rUint(uint value) : base(value) { }
    }

    [Serializable]
    public class rLong : Reactive<long>
    {
        public rLong() : this(default) { }
        public rLong(long value) : base(value) { }
    }

    [Serializable]
    public class rFloat : Reactive<float>
    {
        public rFloat() : this(default) { }
        public rFloat(float value) : base(value) { }
    }

    [Serializable]
    public class rString : Reactive<string>
    {
        public rString() : this(default) { }
        public rString(string value) : base(value) { }
    }

    [Serializable]
    public class rVector2 : Reactive<Vector2>
    {
        public rVector2() : this(default) { }
        public rVector2(Vector2 value) : base(value) { }
    }

    [Serializable]
    public class rSprite : Reactive<Sprite>
    {
        public rSprite() : this(default) { }
        public rSprite(Sprite value) : base(value) { }
    }
}