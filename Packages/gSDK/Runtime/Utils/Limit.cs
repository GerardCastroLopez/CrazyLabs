using System;

[Serializable]
public class Limit<T>
{
    public T Min, Max;


    public Limit()
    {}

    public Limit(T min, T max)
    {
        Min = min;
        Max = max;
    }
}