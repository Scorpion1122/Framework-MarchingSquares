using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public static class VoxelGizmos
    {
        public static void DrawVoxels(Transform transform, ChunkData chunkData, float size)
        {
            for (int i = 0; i < chunkData.resolution * chunkData.resolution; i++)
            {
                DrawVoxel(transform, chunkData, i, chunkData.resolution, size);
            }
        }

        public static void DrawVoxel(Transform transform, ChunkData chunkData, int index, int resolution, float size)
        {
            FillType fillType = chunkData.fillTypes[index];
            float2 offset = chunkData.offsets[index];
            float2 normalX = chunkData.normalsX[index];
            float2 normalY = chunkData.normalsY[index];

            Gizmos.color = GetColor(fillType);

            float2 position = VoxelUtility.IndexToPosition(index, resolution, size) + chunkData.origin;

            Vector3 worldPosition = transform.TransformPoint(new Vector3(position.x, position.y, 0));
            Vector3 offsetPositionX = worldPosition + offset.x * size * transform.right;
            Vector3 offsetPositionY = worldPosition + offset.y * size * transform.up;

            Gizmos.DrawSphere(worldPosition, size * 0.1f);
            Gizmos.DrawLine(worldPosition, offsetPositionX);
            Gizmos.DrawLine(worldPosition, offsetPositionY);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(offsetPositionX, offsetPositionX + new Vector3(normalX.x, normalX.y));
            Gizmos.DrawLine(offsetPositionY, offsetPositionY + new Vector3(normalY.x, normalY.y));

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

//    public static void DrawColliders(Transform transform, ChunkData chunkData)
//    {
//        int offset = 0;
//        for (int i = 0; i < chunkData.colliderLengths.Length; i++)
//        {
//            FillType fillType = chunkData.colliderTypes[i];
//            int length = chunkData.colliderLengths[i];
//
//            Gizmos.color = GetColor(fillType);
//            for (int j = 0; j < length - 1; j++)
//            {
//                int index = offset + j;
//                int nextIndex = offset + j + 1;
//
//                float2 position = chunkData.colliderVertices[index];
//                float2 nextPosition = chunkData.colliderVertices[nextIndex];
//
//                Vector3 worldPosition = transform.TransformPoint(new Vector3(position.x, position.y, 0));
//                Vector3 nextWorldPosition = transform.TransformPoint(new Vector3(nextPosition.x, nextPosition.y, 0));
//                Gizmos.DrawLine(worldPosition, nextWorldPosition);
//            }
//            Gizmos.color = Color.white;
//
//            offset += length;
//        }
//    }
    }
}
