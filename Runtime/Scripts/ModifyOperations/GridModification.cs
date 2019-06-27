using Unity.Mathematics;
using UnityEngine;

public struct GridModification
{
    public ModifierType modifierType;
    public FillType setFilltype;
    public float2 position;
    public float size;

    public Rect GetBounds()
    {
        return new Rect(position, Vector2.one * size);
    }
}
