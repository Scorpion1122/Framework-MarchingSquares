using Unity.Collections;

public static class NativeArrayExtensions
{
    public static T[] ToArray<T>(this NativeList<T> array, int from, int length) where T : struct
    {
        T[] result = new T[length];

        for (int i = 0; i < length; i++)
            result[i] = array[i + from];

        return result;
    }
}
