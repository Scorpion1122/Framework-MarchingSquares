using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ChunkData : IDisposable
{
    //Voxel Data
    public NativeArray<FillType> fillTypes;
    public NativeArray<float2> offsets;

    public JobHandle? jobHandle;

    //Collider Data
    public NativeList<float2> colliderVertices;
    public NativeList<int> colliderLengths;
    public NativeList<FillType> colliderTypes;

    public NativeList<int> processedCache;

    public ChunkData(int resolution)
    {
        //Voxel Data
        fillTypes = new NativeArray<FillType>(resolution * resolution, Allocator.Persistent);
        offsets = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);

        //Collider Data
        colliderVertices = new NativeList<float2>(Allocator.Persistent);
        colliderLengths = new NativeList<int>(Allocator.Persistent);
        colliderTypes = new NativeList<FillType>(Allocator.Persistent);

        processedCache = new NativeList<int>(resolution * resolution, Allocator.Persistent);
    }

    public void ClearTempData()
    {
        colliderVertices.Clear();
        colliderLengths.Clear();
        colliderTypes.Clear();
    }

    public void Dispose()
    {
        fillTypes.Dispose();
        offsets.Dispose();

        colliderVertices.Dispose();
        colliderLengths.Dispose();
        colliderTypes.Dispose();

        processedCache.Dispose();
    }
}
