﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public class ChunkData : IDisposable
    {
        public float2 origin;
        public float size;
        public int resolution;

        //Voxel Data
        public NativeArray<FillType> fillTypes;
        public NativeArray<float2> offsets;
        public NativeArray<float2> normalsX;
        public NativeArray<float2> normalsY;

        //Modifiers
        public NativeList<GridModification> modifiers;

        public JobHandle? jobHandle;
        public List<IChunkJobDependency> dependencies;

        public ChunkData(float2 chunkOrigin, float chunkSize, int chunkResolution)
        {
            resolution = chunkResolution;
            origin = chunkOrigin;
            size = chunkSize;

            //Voxel Data
            fillTypes = new NativeArray<FillType>(resolution * resolution, Allocator.Persistent);
            offsets = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
            normalsX = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);
            normalsY = new NativeArray<float2>(resolution * resolution, Allocator.Persistent);

            //Modifiers
            modifiers = new NativeList<GridModification>(100, Allocator.Persistent);

            dependencies = new List<IChunkJobDependency>();
        }

        public void Dispose()
        {
            fillTypes.Dispose();
            offsets.Dispose();
            normalsX.Dispose();
            normalsY.Dispose();
            modifiers.Dispose();
        }

        public Rect GetBounds()
        {
            return new Rect(origin, new float2(size, size));
        }
    }
}