using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public static class CameraUtility
    {
        public static void GetActiveCameras(ref List<Camera> cameras)
        {
            cameras.Add(Camera.main);
            #if UNITY_EDITOR 
            cameras.Add(SceneView.lastActiveSceneView.camera);
            #endif
        }
        
        public static bool IsCurrentCamera2D(Camera camera)
        {
            return camera.orthographic && camera.transform.forward == Vector3.forward;
        }

        public static void AddChunkRangeInCameraView(Camera camera, int padding, float chunkSize, ref List<int2> chunkIndices)
        {
            if (!IsCurrentCamera2D(camera))
                return;

            Vector3 min = camera.ViewportToWorldPoint(new Vector3(0, 0, 0));
            Vector3 max = camera.ViewportToWorldPoint(new Vector3(1, 1, 0));

            int2 minIndex = ChunkUtility.PositionToChunkIndex(new float2(min.x, min.y), chunkSize);
            int2 maxIndex = ChunkUtility.PositionToChunkIndex(new float2(max.x, max.y), chunkSize);

            for (int x = minIndex.x - padding; x <= maxIndex.x + padding; x++)
            {
                for (int y = minIndex.y - padding; y <= maxIndex.y + padding; y++)
                {
                    chunkIndices.Add(new int2(x, y));
                }
            }
        }
    }
}