using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ModifyFillTypeJob : IJobParallelFor
{
    [ReadOnly] public int resolution;
    [ReadOnly] public float size;
    [ReadOnly] public NativeList<GridModification> modifiers;
    public NativeArray<FillType> fillTypes;

    public void Execute(int index)
    {
        float2 voxelPosition = VoxelUtility.IndexToPosition(index, resolution, size);
        FillType fillType = fillTypes[index];
        for (int i = 0; i < modifiers.Length; i++)
        {
            GridModification modifier = modifiers[i];
            switch (modifier.modifierType)
            {
                case ModifierType.Circle:
                    fillType = RunCircleModifier(fillType, voxelPosition, modifier);
                    break;
                case ModifierType.Square:
                    fillType = RunSquareModifier(fillType, voxelPosition, modifier);
                    break;
            }
        }

        fillTypes[index] = fillType;
    }

    private FillType RunCircleModifier(FillType fillType, float2 voxelPosition, GridModification modifier)
    {
        float2 difference = voxelPosition - modifier.position;
        float distance = math.length(difference);

        //Update Voxel Type
        if (distance < modifier.size)
        {
            return modifier.setFilltype;
        }
        return fillType;
    }

    private FillType RunSquareModifier(FillType fillType, float2 voxelPosition, GridModification modifier)
    {
        float2 min = new float2(modifier.position.x - modifier.size, modifier.position.y - modifier.size);
        float2 max = new float2(modifier.position.x + modifier.size, modifier.position.y + modifier.size);

        bool withinHeight = voxelPosition.y >= min.y && voxelPosition.y <= max.y;
        bool withinLength = voxelPosition.x >= min.x && voxelPosition.x <= max.x;

        if (withinHeight && withinLength)
        {
            return modifier.setFilltype;
        }
        return fillType;
    }
}
