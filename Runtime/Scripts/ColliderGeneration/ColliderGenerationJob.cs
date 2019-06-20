/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ColliderGenerationJob : IJob
{
    [ReadOnly] public int resolution;
    [ReadOnly] public float size;
    [ReadOnly] public NativeArray<FillType> fillTypes;
    [ReadOnly] public NativeArray<float2> offsets;

    [ReadOnly] public NativeArray<FillType> generateForFillTypes;

    //Types / Indices
    public NativeMultiHashMap<int, int> processed;

    [WriteOnlye] public NativeList<float2> vertices;
    [WriteOnlye] public NativeList<int> startIndex;
    [WriteOnlye] public NativeList<FillType> fillType;

    public void Execute()
    {
        for (int i = 0; i < fillTypes.Length; i++)
        {
            Execute(i);
        }
    }

    private void Execute(int index)
    {
        for (int i = 0; i < generateForFillTypes.Length; i++)
        {
            if (processed.Contains(i, index))
                continue;

            Execute(index, (FillType)i);
        }
    }

    private void Execute(int index, FillType fillType)
    {
        int topIndex = index + resolution;
        int topRightIndex = index + resolution + 1;
        int rightIndex = index + 1;

        int voxelType = VoxelUtility.GetVoxelShape(
            fillType,
            index,
            fillTypes,
            resolution);

        processed.Add((int)fillType, index);

        if (voxelType == 0)
            return;
    }
}
*/