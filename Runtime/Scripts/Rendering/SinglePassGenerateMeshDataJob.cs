using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct SinglePassGenerateMeshDataJob : IJob
    {
        [ReadOnly] public int resolution;
        [ReadOnly] public float size;
        [ReadOnly] public NativeArray<FillType> generateForFillTypes;
        [ReadOnly] public NativeArray<FillType> fillTypes;
        [ReadOnly] public NativeArray<float2> offsets;

        public NativeList<float2> vertices;
        public NativeList<int> triangleIndices;
        public NativeList<int> triangleLengths;

        public void Execute()
        {
            VertexCache cache = new VertexCache(resolution);

            int previousLength = 0;
            for (int i = 0; i < generateForFillTypes.Length; i++)
            {
                Execute(generateForFillTypes[i], cache);
                
                int length = triangleIndices.Length - previousLength;
                triangleLengths.Add(length);
                previousLength = length;
            }
            cache.Dispose();
        }

        private void Execute(FillType fillType, VertexCache cache)
        {
            for (int i = 0; i < fillTypes.Length; i++)
            {
                //Last voxel of a row
                if (i % resolution == resolution - 1)
                {
                    cache.Swap();
                    continue;
                }

                Execute(i, fillType, cache);
            }
        }

        private void Execute(int index, FillType fillType, VertexCache cache)
        {
            int topIndex = index + resolution;
            int topRightIndex = index + resolution + 1;
            int rightIndex = index + 1;

            FillType currentFill = fillTypes[index];
            FillType topFill = GetNeightbourFillType(topIndex);
            FillType topRightFill = GetNeightbourFillType(topRightIndex);
            FillType rightFill = GetNeightbourFillType(rightIndex);

            int voxelType = VoxelUtility.GetVoxelShape(
                fillType,
                currentFill,
                topFill,
                topRightFill,
                rightFill);

            if (voxelType == 0)
                return;

            float2 curPosition = VoxelUtility.IndexToPosition(index, resolution, size);
            float2 topPosition = VoxelUtility.IndexToPosition(topIndex, resolution, size);
            float2 topRightPosition = VoxelUtility.IndexToPosition(topRightIndex, resolution, size);
            float2 rightPosition = VoxelUtility.IndexToPosition(rightIndex, resolution, size);

            float2 currentOffset = offsets[index];
            float2 topOffset = GetNeightbourOffset(topIndex);
            float2 rightOffset = GetNeightbourOffset(rightIndex);

            bool firstRow = index < resolution;
            bool firstColumn = index % resolution == 0;
            int curCacheI = (index % resolution) * 2;
            int midCacheI = index % resolution;

            switch (voxelType)
            {
                //None
                case 0:
                    break;

                #region One Corner
                case 1:
                    if (firstRow && firstColumn)
                    {
                        vertices.Add(curPosition);
                        cache.prevRow[curCacheI] = vertices.Length - 1; //v1
                    }
                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y));
                        cache.midRow[midCacheI] = vertices.Length - 1; //v2
                    }
                    if (firstRow)
                    {
                        vertices.Add(curPosition + new float2(currentOffset.x, 0));
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1; //v3
                    }

                    AddTriangle(
                        cache.prevRow[curCacheI], 
                        cache.midRow[midCacheI], 
                        cache.prevRow[curCacheI + 1]);
                    break;
                case 2:
                    vertices.Add(topPosition); //v1
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v2
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;

                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v3
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }
                    
                    AddTriangle(
                        cache.nextRow[curCacheI], 
                        cache.nextRow[curCacheI + 1], 
                        cache.midRow[midCacheI]);
                    break;
                case 4:
                    vertices.Add(topRightPosition); //v1
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v2
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v3
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    AddTriangle(
                        cache.nextRow[curCacheI + 2], 
                        cache.midRow[midCacheI + 1], 
                        cache.nextRow[curCacheI + 1]);
                    break;
                case 8:
                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v1
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v2
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v3
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    AddTriangle(
                        cache.prevRow[curCacheI + 2], 
                        cache.prevRow[curCacheI + 1], 
                        cache.midRow[midCacheI + 1]);
                    break;
                #endregion One Corner
                
                #region Two Corners
                case 3:
                    if (firstRow)
                    {
                        vertices.Add(curPosition); //v1
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v4
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }
                    
                    vertices.Add(topPosition); //v2
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v3
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    AddQuad(
                        cache.prevRow[curCacheI],
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 1],
                        cache.prevRow[curCacheI + 1]);
                    break;
                case 6:
                    vertices.Add(topPosition); //v1
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topRightPosition); //v2
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v3
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v4
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }
                    
                    AddQuad(
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 2],
                        cache.midRow[midCacheI + 1],
                        cache.midRow[midCacheI]);
                    break;
                case 12:
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v1
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    vertices.Add(topRightPosition); //v2
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;

                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v3
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v4
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }
                    
                    AddQuad(
                        cache.nextRow[curCacheI + 1],
                        cache.nextRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 1]);
                    break;
                case 9:
                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v1
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                    }

                    if (firstColumn)
                    {
                        vertices.Add(curPosition); //v2
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v3
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v4
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    AddQuad(
                        cache.prevRow[curCacheI + 2],
                        cache.prevRow[curCacheI],
                        cache.midRow[midCacheI],
                        cache.midRow[midCacheI + 1]);
                    break;
                #endregion Two Corners

                //Opposite Corners
                case 5:
                    if (firstRow)
                    {
                        vertices.Add(curPosition); //v1
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v3
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }

                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v2
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }
                    
                    vertices.Add(topRightPosition); //v4
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;
                        
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v5
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v6
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    AddTriangle(
                        cache.prevRow[curCacheI],
                        cache.midRow[midCacheI],
                        cache.prevRow[curCacheI + 1]);
                    AddTriangle(
                        cache.nextRow[curCacheI + 2],
                        cache.midRow[midCacheI + 1],
                        cache.nextRow[curCacheI + 1]);
                    break;
                case 10:
                    vertices.Add(topPosition); //v1
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v2
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;

                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v3
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }

                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v4
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                        
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v5
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v6
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;

                    AddTriangle(
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 1],
                        cache.midRow[midCacheI]);
                    AddTriangle(
                        cache.prevRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 1],
                        cache.midRow[midCacheI + 1]);
                    break;

                #region Three Corners
                case 7:
                    vertices.Add(topPosition); //v1
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topRightPosition); //v2
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v3
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;

                    if (firstRow)
                    {
                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v4
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                        
                        vertices.Add(curPosition); //v5
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                    }
                    
                    AddPentagon(
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 2],
                        cache.midRow[midCacheI + 1],
                        cache.prevRow[curCacheI + 1],
                        cache.prevRow[curCacheI]);
                    break;
                case 14:
                    vertices.Add(topRightPosition); //v1
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;

                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v2
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;

                        vertices.Add(curPosition + new float2(currentOffset.x, 0)); //v3
                        cache.prevRow[curCacheI + 1] = vertices.Length - 1;
                    }

                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v4
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }

                    vertices.Add(topPosition); //v5
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    AddPentagon(
                        cache.nextRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 1],
                        cache.midRow[midCacheI],
                        cache.nextRow[curCacheI]);
                    break;
                case 13:
                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v1
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                        
                        vertices.Add(curPosition); //v2
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                    }

                    if (firstColumn)
                    {
                        vertices.Add(curPosition + new float2(0, currentOffset.y)); //v3
                        cache.midRow[midCacheI] = vertices.Length - 1;
                    }
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v4
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    vertices.Add(topRightPosition); //v5
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;
                    
                    AddPentagon(
                        cache.prevRow[curCacheI + 2],
                        cache.prevRow[curCacheI],
                        cache.midRow[midCacheI],
                        cache.nextRow[curCacheI + 1],
                        cache.nextRow[curCacheI + 2]);
                    break;
                case 11:
                    if (firstRow)
                    {
                        vertices.Add(curPosition); //v1
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                        
                        vertices.Add(rightPosition); //v5
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                    }
                    
                    vertices.Add(topPosition); //v2
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    
                    vertices.Add(topPosition + new float2(topOffset.x, 0)); //v3
                    cache.nextRow[curCacheI + 1] = vertices.Length - 1;
                    
                    vertices.Add(rightPosition + new float2(0, rightOffset.y)); //v4
                    cache.midRow[midCacheI + 1] = vertices.Length - 1;
                    
                    AddPentagon(
                        cache.prevRow[curCacheI],
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 1],
                        cache.midRow[midCacheI + 1],
                        cache.prevRow[curCacheI + 2]);
                    break;
                #endregion Three Corners
                
                #region All Corners
                case 15:
                    if (firstColumn)
                    {
                        vertices.Add(curPosition); //v1
                        cache.prevRow[curCacheI] = vertices.Length - 1;
                    }
                    
                    vertices.Add(topPosition); //v2
                    cache.nextRow[curCacheI] = vertices.Length - 1;
                    vertices.Add(topRightPosition); //v3
                    cache.nextRow[curCacheI + 2] = vertices.Length - 1;

                    if (firstRow)
                    {
                        vertices.Add(rightPosition); //v4
                        cache.prevRow[curCacheI + 2] = vertices.Length - 1;
                    }

                    AddQuad(
                        cache.prevRow[curCacheI],
                        cache.nextRow[curCacheI],
                        cache.nextRow[curCacheI + 2],
                        cache.prevRow[curCacheI + 2]);
                    break;
                #endregion All Corners
            }
        }

        private void AddPentagon(int v1, int v2, int v3, int v4, int v5)
        {
            AddTriangle(v1, v2, v3);
            AddTriangle(v1, v3, v4);
            AddTriangle(v1, v4, v5);
        }

        private void AddQuad(int v1, int v2, int v3, int v4)
        {
            AddTriangle(v1, v2, v3);
            AddTriangle(v1, v3, v4);
        }

        private void AddTriangle(int v1, int v2, int v3)
        {
            triangleIndices.Add(v1);
            triangleIndices.Add(v2);
            triangleIndices.Add(v3);
        }

        private FillType GetNeightbourFillType(int index)
        {
            if (index >= fillTypes.Length)
                return FillType.None;
            return fillTypes[index];
        }

        private float2 GetNeightbourOffset(int index)
        {
            if (index >= offsets.Length)
                return float2.zero;
            return offsets[index];
        }
    }
}
