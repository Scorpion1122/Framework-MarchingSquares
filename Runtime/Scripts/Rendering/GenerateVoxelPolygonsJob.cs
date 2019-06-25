using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateVoxelPolygonsJob : IJobParallelFor
{
    [ReadOnly] public int resolution;
    [ReadOnly] public float size;
    [ReadOnly] public NativeArray<FillType> generateForFillTypes;
    [ReadOnly] public NativeArray<FillType> fillTypes;
    [ReadOnly] public NativeArray<float2> offsets;
    [WriteOnly] public NativeMultiHashMap<int, Polygon>.Concurrent polygons;

    //  top ----- top right
    //   |           |
    //   |           |
    // current ---- right
    public void Execute(int index)
    {
        for (int i = 0; i < generateForFillTypes.Length; i++)
        {
            Execute(index, generateForFillTypes[i]);
        }
    }

    private void Execute(int index, FillType fillType)
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

        switch (voxelType)
        {
            //None
            case 0:
                break;

            //One Corner
            case 1:
                AddOneCornerPolygon(
                    fillType,
                    curPosition,
                    curPosition + new float2(0, currentOffset.y),
                    curPosition + new float2(currentOffset.x, 0));
                break;
            case 2:
                AddOneCornerPolygon(
                    fillType,
                    topPosition,
                    topPosition + new float2(topOffset.x, 0),
                    curPosition + new float2(0, currentOffset.y));
                break;
            case 4:
                AddOneCornerPolygon(
                    fillType,
                    topRightPosition,
                    rightPosition + new float2(0, rightOffset.y),
                    topPosition + new float2(topOffset.x, 0));
                break;
            case 8:
                AddOneCornerPolygon(
                    fillType,
                    rightPosition,
                    curPosition + new float2(currentOffset.x, 0),
                    rightPosition + new float2(0, rightOffset.y));
                break;

            //Two Corners
            case 3:
                AddTwoCornerPolygon(
                    fillType,
                    curPosition,
                    topPosition,
                    topPosition + new float2(topOffset.x, 0),
                    curPosition + new float2(currentOffset.x, 0));
                break;
            case 6:
                AddTwoCornerPolygon(
                    fillType,
                    topPosition,
                    topRightPosition,
                    rightPosition + new float2(0, rightOffset.y),
                    curPosition + new float2(0, currentOffset.y));
                break;
            case 12:
                AddTwoCornerPolygon(
                    fillType,
                    topRightPosition,
                    rightPosition,
                    curPosition + new float2(currentOffset.x, 0),
                    topPosition + new float2(topOffset.x, 0));
                break;
            case 9:
                AddTwoCornerPolygon(
                    fillType,
                    rightPosition,
                    curPosition,
                    curPosition + new float2(0, currentOffset.y),
                    rightPosition + new float2(0, rightOffset.y));
                break;

            //Opposite Corners
            case 5:
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
                break;

            //Three Corners
            case 7:
                AddThreeCornersPolygon(
                    fillType,
                    topPosition,
                    topRightPosition,
                    rightPosition + new float2(0, rightOffset.y),
                    curPosition + new float2(currentOffset.x, 0),
                    curPosition);
                break;
            case 14:
                AddThreeCornersPolygon(
                    fillType,
                    topRightPosition,
                    rightPosition,
                    curPosition + new float2(currentOffset.x, 0),
                    curPosition + new float2(0, currentOffset.y),
                    topPosition);
                break;
            case 13:
                AddThreeCornersPolygon(
                    fillType,
                    rightPosition,
                    curPosition,
                    curPosition + new float2(0, currentOffset.y),
                    topPosition + new float2(topOffset.x, 0),
                    topRightPosition);
                break;
            case 11:
                AddThreeCornersPolygon(
                    fillType,
                    curPosition,
                    topPosition,
                    topPosition + new float2(topOffset.x, 0),
                    rightPosition + new float2(0, rightOffset.y),
                    rightPosition);
                break;

            //All Corners
            case 15:
                AddAllCornerPolygon(
                    fillType,
                    curPosition,
                    topPosition,
                    topRightPosition,
                    rightPosition);
                break;
        }
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

    private void AddOneCornerPolygon(FillType fillType, float2 one, float2 two, float2 three)
    {
        polygons.Add((int)fillType,
            new Polygon()
        {
            type = PolygonType.OneCorner,
            one = one,
            two = two,
            three = three,
        });
    }

    private void AddTwoCornerPolygon(FillType fillType, float2 one, float2 two, float2 three, float2 four)
    {
        polygons.Add((int)fillType,
            new Polygon()
            {
                type = PolygonType.TwoCorners,
                one = one,
                two = two,
                three = three,
                four = four,
            });
    }

    private void AddCrossCornerPolygon(FillType fillType, float2 one, float2 two, float2 three, float2 four, float2 five, float2 six)
    {
        polygons.Add((int)fillType,
            new Polygon()
            {
                type = PolygonType.CrossCorners,
                one = one,
                two = two,
                three = three,
                four = four,
                five = five,
                six = six,
            });
    }

    private void AddThreeCornersPolygon(FillType fillType, float2 one, float2 two, float2 three, float2 four, float2 five)
    {
        polygons.Add((int)fillType,
            new Polygon()
            {
                type = PolygonType.ThreeCorners,
                one = one,
                two = two,
                three = three,
                four = four,
                five = five,
            });
    }

    private void AddAllCornerPolygon(FillType fillType, float2 one, float2 two, float2 three, float2 four)
    {
        polygons.Add((int)fillType,
            new Polygon()
            {
                type = PolygonType.AllCorners,
                one = one,
                two = two,
                three = three,
                four = four,
            });
    }
}
