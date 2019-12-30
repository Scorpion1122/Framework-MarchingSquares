using UnityEngine;
using System.Collections;

namespace UnityEngine
{
    public static class BoundsIntExtensions
    {
        public static BoundsInt GetOverlapArea(this BoundsInt bounds, BoundsInt otherBounds)
        {
            BoundsInt result = bounds;
            result.xMin = Mathf.Max(bounds.xMin, otherBounds.xMin);
            result.yMin = Mathf.Max(bounds.yMin, otherBounds.yMin);
            result.xMax = Mathf.Min(bounds.xMax, otherBounds.xMax);
            result.yMax = Mathf.Min(bounds.yMax, otherBounds.yMax);
            return result;
        }
    }
}
