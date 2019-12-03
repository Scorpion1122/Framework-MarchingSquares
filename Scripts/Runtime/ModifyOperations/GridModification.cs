using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public struct GridModification
    {
        public ModifierShape ModifierShape;
        public ModifierType modifierType;
        public FillType setFilltype;
        public float2 position;
        public float size;

        public Rect GetBounds()
        {
            float2 rectSize = new float2(1, 1) * size * 2;
            float2 rectPosition = position - rectSize * 0.5f;
            //Rect position is bottom left as pivot, not the center
            return new Rect(rectPosition, rectSize);
        }
    }
}
