using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ColliderGenerationJob : IJob
{
    [ReadOnly] public int resolution;
    [ReadOnly] public float size;
    [ReadOnly] public NativeArray<FillType> fillTypes;
    [ReadOnly] public NativeArray<float2> offsets;

    [ReadOnly] public NativeArray<FillType> supportedFillTypes;

    //Types / Indices
    //public NativeMultiHashMap<int, int> processed;
    public NativeList<int> processed;

    [WriteOnly] public NativeList<float2> vertices;
    [WriteOnly] public NativeList<FillType> colliderFillTypes;
    [WriteOnly] public NativeList<int> lengths;
    private float2 startPosition;
    private int currentLength;
    private int startIndex;

    public void Execute()
    {
        for (int i = 0; i < supportedFillTypes.Length; i++)
        {
            Execute(supportedFillTypes[i]);
        }
    }

    private void Execute(FillType fillType)
    {
        processed.Clear();
        for (int i = 0; i < fillTypes.Length; i++)
        {
            if (VoxelUtility.IsNeighbourChunkVoxel(i, resolution))
                continue;
            
            if (processed.Contains(i))
                continue;

            Execute(i, fillType);
        }
    }

    private void Execute(int index, FillType fillType)
    {
        int voxelType = VoxelUtility.GetVoxelShape(
            index,
            fillType,
            fillTypes,
            resolution);

        if (voxelType == 0 || voxelType == 15) //None or all corners
            return;

        currentLength = 0;
        startIndex = index;
        startIndex = FindStartIndex(index, fillType);
        
        voxelType = VoxelUtility.GetVoxelShape(
            startIndex,
            fillType,
            fillTypes,
            resolution);
        
        processed.Add(startIndex);
        MoveToNextVoxelCW(startIndex, fillType, voxelType);

        if (currentLength != 0)
        {
            this.lengths.Add(currentLength);
            this.colliderFillTypes.Add(fillType);
        }
    }

    private void MoveToNextVoxel(int index, FillType fillType)
    {
        if (index == startIndex)
        {
            AddVertex(startPosition);
            return;
        }

        int voxelType = VoxelUtility.GetVoxelShape(
            index,
            fillType,
            fillTypes,
            resolution);
        
        if (voxelType == 0 || voxelType == 15) //None or all corners
            return;

        processed.Add(index);
        MoveToNextVoxelCW(index, fillType, voxelType);
    }

    //Clock wise
    private void MoveToNextVoxelCW(int index, FillType fillType, int voxelType)
    {
        int topIndex = index + resolution;
        int topRightIndex = index + resolution + 1;
        int rightIndex = index + 1;
        
        float2 curPosition = VoxelUtility.IndexToPosition(index, resolution, size);
        float2 topPosition = VoxelUtility.IndexToPosition(topIndex, resolution, size);
        float2 topRightPosition = VoxelUtility.IndexToPosition(topRightIndex, resolution, size);
        float2 rightPosition = VoxelUtility.IndexToPosition(rightIndex, resolution, size);

        float2 currentOffset = offsets[index];
        float2 topOffset = VoxelUtility.GetNeightbourOffset(topIndex, offsets);
        float2 rightOffset = VoxelUtility.GetNeightbourOffset(rightIndex, offsets);

        int2 directionToNextVoxel = int2.zero;
        switch (voxelType)
        {
            //None
            case 0:
                return;

            //One Corner, Bottom Left
            case 1:
                AddVertex(curPosition + new float2(currentOffset.x, 0));
                directionToNextVoxel = new int2(0, -1);
                break;
            //One Corner, Top Left
            case 2:
                AddVertex(curPosition + new float2(0, currentOffset.y));
                directionToNextVoxel = new int2(-1, 0);
                break;
            //One Corner, Top Right
            case 4:
                AddVertex(topPosition + new float2(topOffset.x, 0));
                directionToNextVoxel = new int2(0, 1);
                break;
            //One Corner, Bottom Right
            case 8:
                AddVertex(rightPosition + new float2(0, rightOffset.y));
                directionToNextVoxel = new int2(1, 0);
                break;

            //Two Corners, Left
            case 3:
                AddVertex(curPosition + new float2(currentOffset.x, 0));
                directionToNextVoxel = new int2(0, -1);
                break;
            //Two Corners, Top
            case 6:
                AddVertex(curPosition + new float2(0, currentOffset.y));
                directionToNextVoxel = new int2(-1, 0);
                break;
            //Two Corners, Right
            case 12:
                AddVertex(topPosition + new float2(topOffset.x, 0));
                directionToNextVoxel = new int2(0, 1);
                break;
            //Two Corners, Bottom
            case 9:
                AddVertex(rightPosition + new float2(0, rightOffset.y));
                directionToNextVoxel = new int2(1, 0);
                break;

            //Opposite Corners
            /*case 5:
                AddCrossCornerPolygon(
                    fillType,
                    curPosition,
                    curPosition + new float2(0, currentOffset.y),
                    curPosition + new float2(currentOffset.x, 0),
                    topRightPosition,
                    rightPosition + new float2(0, rightOffset.y),
                    topPosition + new float2(topOffset.x, 0));
                break;
            case 10:
                AddCrossCornerPolygon(
                    fillType,
                    topPosition,
                    topPosition + new float2(topOffset.x, 0),
                    curPosition + new float2(0, currentOffset.y),
                    rightPosition,
                    curPosition + new float2(currentOffset.x, 0),
                    rightPosition + new float2(0, rightOffset.y));
                break;*/

            //Three Corners
            case 7:
                AddVertex(curPosition + new float2(currentOffset.x, 0));
                directionToNextVoxel = new int2(0, -1);
                break;
            case 14:
                AddVertex(curPosition + new float2(0, currentOffset.y));
                directionToNextVoxel = new int2(-1, 0);
                break;
            case 13:
                AddVertex(topPosition + new float2(topOffset.x, 0));
                directionToNextVoxel = new int2(0, 1);
                break;
            case 11:
                AddVertex(rightPosition + new float2(0, rightOffset.y));
                directionToNextVoxel = new int2(1, 0);
                break;
        }
        
        if (directionToNextVoxel.x == 0 && directionToNextVoxel.y == 0)
            return;
        
        int2 newPosition = VoxelUtility.IndexToIndex2(index, resolution) + directionToNextVoxel;
        if (newPosition.x < 0 
            || newPosition.x > resolution
            || newPosition.y < 0
            || newPosition.y > resolution)
            return;

        MoveToNextVoxel(VoxelUtility.Index2ToIndex(newPosition, resolution), fillType);
    }

    private int FindStartIndex(int index, FillType fillType)
    {
        int voxelType = VoxelUtility.GetVoxelShape(
            index,
            fillType,
            fillTypes,
            resolution);
        
        int2 directionToNextVoxel = int2.zero;
        switch (voxelType)
        {
            //None
            default:
                throw new IndexOutOfRangeException();

            //One Corner, Bottom Left
            case 1:
                directionToNextVoxel = new int2(-1, 0);
                break;
            //One Corner, Top Left
            case 2:
                directionToNextVoxel = new int2(0, 1);
                break;
            //One Corner, Top Right
            case 4:
                directionToNextVoxel = new int2(1, 0);
                break;
            //One Corner, Bottom Right
            case 8:
                directionToNextVoxel = new int2(0, -1);
                break;
            
            //Two Corners, Left
            case 3:
                directionToNextVoxel = new int2(0, 1);
                break;
            //Two Corners, Top
            case 6:
                directionToNextVoxel = new int2(1, 0);
                break;
            //Two Corners, Right
            case 12:
                directionToNextVoxel = new int2(0, -1);
                break;
            //Two Corners, Bottom
            case 9:
                directionToNextVoxel = new int2(-1, 0);
                break;

            case 5:
                return index;
            case 10:
                return index;
            //Opposite Corners
            /*case 5:
                AddCrossCornerPolygon(
                    fillType,
                    curPosition,
                    curPosition + new float2(0, currentOffset.y),
                    curPosition + new float2(currentOffset.x, 0),
                    topRightPosition,
                    rightPosition + new float2(0, rightOffset.y),
                    topPosition + new float2(topOffset.x, 0));
                break;
            case 10:
                AddCrossCornerPolygon(
                    fillType,
                    topPosition,
                    topPosition + new float2(topOffset.x, 0),
                    curPosition + new float2(0, currentOffset.y),
                    rightPosition,
                    curPosition + new float2(currentOffset.x, 0),
                    rightPosition + new float2(0, rightOffset.y));
                break;*/

            //Three Corners
            //Right (Empty)
            case 7:
                directionToNextVoxel = new int2(1, 0);
                break;
            //Left (Empty)
            case 14:
                directionToNextVoxel = new int2(0, -1);
                break;
            //Top Left (Empty)
            case 13:
                directionToNextVoxel = new int2(-1, 0);
                break;
            //Top Right (Empty)
            case 11:
                directionToNextVoxel = new int2(0, 1);
                break;
        }
        
        int2 newPosition = VoxelUtility.IndexToIndex2(index, resolution) + directionToNextVoxel;
        if (newPosition.x < 0 
            || newPosition.x > resolution
            || newPosition.y < 0
            || newPosition.y > resolution)
            return index;

        int nextIndex = VoxelUtility.Index2ToIndex(newPosition, resolution);
        if (nextIndex == startIndex)
            return startIndex;
        
        return FindStartIndex(nextIndex, fillType);
    }

    private void AddVertex(float2 vertex)
    {
        if (currentLength == 0)
            startPosition = vertex;
        
        vertices.Add(vertex);
        currentLength++;
    }
}
