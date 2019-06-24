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

    public ChunkData(int resolution)
    {
        //Voxel Data
        fillTypes = new NativeArray<FillType>(resolution * resolution, Allocator.Persistent);
        offsets = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
    }

    public void Dispose()
    {
        fillTypes.Dispose();
        offsets.Dispose();
    }
}
