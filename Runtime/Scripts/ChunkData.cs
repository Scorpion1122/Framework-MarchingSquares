using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ChunkData : IDisposable
{
    public NativeArray<FillType> fillTypes;
    public NativeArray<float2> offsets;

    public NativeMultiHashMap<int, Polygon> polygons;
    public NativeList<Vector3> vertices;
    public NativeList<int> triangleIndices;
    public NativeList<int> triangleLengths;

    public ChunkData(int resolution)
    {
        fillTypes = new NativeArray<FillType>(resolution * resolution, Allocator.Persistent);
        offsets = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
        polygons = new NativeMultiHashMap<int, Polygon>(1000, Allocator.Persistent);
        vertices = new NativeList<Vector3>(resolution * resolution, Allocator.Persistent);
        triangleIndices = new NativeList<int>(resolution * resolution, Allocator.Persistent);
        triangleLengths = new NativeList<int>(resolution * resolution, Allocator.Persistent);
    }

    public void ClearTempData()
    {
        polygons.Clear();
        vertices.Clear();
        triangleIndices.Clear();
        triangleLengths.Clear();
    }

    public void Dispose()
    {
        fillTypes.Dispose();
        offsets.Dispose();
        polygons.Dispose();
        vertices.Dispose();
        triangleIndices.Dispose();
        triangleLengths.Dispose();
    }
}
