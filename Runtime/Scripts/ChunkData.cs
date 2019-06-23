using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ChunkData : IDisposable
{
    //Voxel Data
    public NativeArray<FillType> fillTypes;
    public NativeArray<float2> offsets;

    //Mesh Data
    public NativeMultiHashMap<int, Polygon> polygons;
    public NativeList<Vector3> vertices;
    public NativeList<int> triangleIndices;
    public NativeList<int> triangleLengths;
    
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
        
        //Mesh Data
        polygons = new NativeMultiHashMap<int, Polygon>(1000, Allocator.Persistent);
        vertices = new NativeList<Vector3>(resolution * resolution, Allocator.Persistent);
        triangleIndices = new NativeList<int>(resolution * resolution, Allocator.Persistent);
        triangleLengths = new NativeList<int>(resolution * resolution, Allocator.Persistent);
        
        //Collider Data
        colliderVertices = new NativeList<float2>(Allocator.Persistent);
        colliderLengths = new NativeList<int>(Allocator.Persistent);
        colliderTypes = new NativeList<FillType>(Allocator.Persistent);
        
        processedCache = new NativeList<int>(resolution * resolution, Allocator.Persistent);
    }

    public void ClearTempData()
    {
        polygons.Clear();
        vertices.Clear();
        triangleIndices.Clear();
        triangleLengths.Clear();
        
        colliderVertices.Clear();
        colliderLengths.Clear();
        colliderTypes.Clear();
    }

    public void Dispose()
    {
        fillTypes.Dispose();
        offsets.Dispose();
        polygons.Dispose();
        vertices.Dispose();
        triangleIndices.Dispose();
        triangleLengths.Dispose();

        colliderVertices.Dispose();
        colliderLengths.Dispose();
        colliderTypes.Dispose();
        
        processedCache.Dispose();
    }
}
