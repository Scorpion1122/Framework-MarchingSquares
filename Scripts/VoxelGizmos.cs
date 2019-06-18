using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class VoxelGizmos
{
    public static void DrawVoxels(Transform transform, ChunkData chunkData, int resolution, float size)
    {
        for (int i = 0; i < resolution * resolution; i++)
        {
            DrawVoxel(transform, chunkData, i, resolution, size);
        }
    }

    public static void DrawVoxel(Transform transform, ChunkData chunkData, int index, int resolution, float size)
    {
        FillType fillType = chunkData.fillTypes[index];
        float2 offset = chunkData.offsets[index];

        Gizmos.color = GetColor(fillType);

        float2 position = VoxelUtility.IndexToPosition(index, resolution, size);

        Vector3 worldPosition = transform.TransformPoint(new Vector3(position.x, position.y, 0));
        Gizmos.DrawSphere(worldPosition, size * 0.1f);
        Gizmos.DrawLine(worldPosition, worldPosition + offset.y * size * transform.up);
        Gizmos.DrawLine(worldPosition, worldPosition + offset.x * size * transform.right);
        Gizmos.color = Color.white;
    }

    private static Color GetColor(FillType fillType)
    {
        switch (fillType)
        {
            case FillType.None:
                return Color.white;
            case FillType.TypeOne:
                return Color.black;
            case FillType.TypeTwo:
                return Color.blue;
            default:
                throw new ArgumentOutOfRangeException(nameof(fillType), fillType, null);
        }
    }
}
