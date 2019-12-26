using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Thijs.Framework.MarchingSquares
{
    public class ChunkData : IDisposable
    {
        public float2 Origin { get; private set; }
        public float Size { get; private set; }
        public int Resolution { get; private set; }
        public bool Initialized { get; set; }

        //Voxel Data
        public NativeArray<FillType> fillTypes;
        public NativeArray<float2> offsets;
        public NativeArray<float2> normalsX;
        public NativeArray<float2> normalsY;

        //Modifiers
        public NativeList<GridModification> modifiers;

        public JobHandle? jobHandle;
        public ChunkJobDependencyGraph dependencies;

        public ChunkData(float2 chunkOrigin, float chunkSize, int chunkResolution)
        {
            Resolution = chunkResolution;
            Origin = chunkOrigin;
            Size = chunkSize;

            //Voxel Data
            fillTypes = new NativeArray<FillType>(Resolution * Resolution, Allocator.Persistent);
            offsets = new NativeArray<float2>(Resolution * Resolution, Allocator.Persistent);
            normalsX = new NativeArray<float2>(Resolution * Resolution, Allocator.Persistent);
            normalsY = new NativeArray<float2>(Resolution * Resolution, Allocator.Persistent);

            //Modifiers
            modifiers = new NativeList<GridModification>(100, Allocator.Persistent);

            dependencies = new ChunkJobDependencyGraph();
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
            return new Rect(Origin, new float2(Size, Size));
        }
    }
}
