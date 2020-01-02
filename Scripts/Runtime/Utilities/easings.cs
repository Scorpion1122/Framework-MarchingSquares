using UnityEngine;
using System.Collections;

namespace Unity.Mathematics
{
    public static class easings
    {
        public static float EaseInQuad(float number)
        {
            return number * number;
        }

        public static float EaseOutQuade(float number)
        {
            return 1 - (1 - number) * (1 - number);
        }

        public static float EaseInOutQuad(float number)
        {
            if (number < 0.5f)
                return 2 * number * number;
            return 1 - math.pow(-2 * number + 2, 2) / 2;
        }

        public static float EaseInCubic(float number)
        {
            return number * number * number;
        }

        public static float EaseOutCubic(float number)
        {
            return 1 - math.pow(1 - number, 3);
        }

        public static float EaseInOutCubic(float number)
        {
            if (number < 0.5f)
                return 4 * number * number * number;
            return 1 - math.pow(-2 * number + 2, 3) / 2;
        }

        public static float EaseInQuart(float number)
        {
            return number * number * number * number;
        }
    }
}
