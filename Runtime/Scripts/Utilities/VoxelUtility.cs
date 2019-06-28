using Unity.Collections;
using Unity.Mathematics;

// (0,0) is left bottom, (max,max) is right top
//
public static class VoxelUtility
{
    public const int NATIVE_CACHE_SIZE = 16384;

        public static float2 IndexToPosition(int index, int resolution, float size)
        {
                float x = math.floor(index % resolution) * size;
                float y = math.floor((index - x) / resolution) * size;
                return new float2(x, y);
        }

        public static int2 IndexToIndex2(int index, int resolution)
        {
                int x = (int)math.floor(index % resolution);
                int y = (int)math.floor((index - x) / resolution);
                return new int2(x, y);
        }

        public static int Index2ToIndex(int2 index, int resolution)
        {
                return index.x + index.y * resolution;
        }

        //Neighbour chunk voxels are duplicate voxels that share data with the neighbouring chunk
        //This is only on the top and left side of the chunk
        public static bool IsNeighbourChunkVoxel(int index, int resolution)
        {
            int2 index2 = IndexToIndex2(index, resolution);
            return index2.x == resolution - 1 || index2.y == resolution - 1;
        }

        //None
        //0:  FillType.None, FillType.None, FillType.None, FillType.None

        //One Corner
        //1:  FillType.TypeOne, FillType.None, FillType.None, FillType.None
        //2:  FillType.None, FillType.TypeOne, FillType.None, FillType.None
        //4:  FillType.None, FillType.None, FillType.TypeOne, FillType.None
        //8:  FillType.None, FillType.None, FillType.None, FillType.TypeOne

        //Two Corners
        //3:  FillType.TypeOne, FillType.TypeOne, FillType.None, FillType.None
        //6:  FillType.None, FillType.TypeOne, FillType.TypeOne, FillType.None
        //12: FillType.None, FillType.None, FillType.TypeOne, FillType.TypeOne
        //9:  FillType.TypeOne, FillType.None, FillType.None, FillType.TypeOne

        //Opposite Corners
        //5:  FillType.TypeOne, FillType.None, FillType.TypeOne, FillType.None
        //10: FillType.None, FillType.TypeOne, FillType.None, FillType.TypeOne

        //Three Corners
        //7:  FillType.TypeOne, FillType.TypeOne, FillType.TypeOne, FillType.None
        //14: FillType.None, FillType.TypeOne, FillType.TypeOne, FillType.TypeOne
        //13: FillType.TypeOne, FillType.None, FillType.TypeOne, FillType.TypeOne
        //11: FillType.TypeOne, FillType.TypeOne, FillType.None, FillType.TypeOne

        //All Corners
        //15:  FillType.TypeOne, FillType.TypeOne, FillType.TypeOne, FillType.TypeOne, FillType.TypeOne
        public static short GetVoxelShape(
                FillType compareType, FillType bottomLeft, FillType topLeft, FillType topRight, FillType bottomRight)
        {
                short result = 0;
                if (compareType == bottomLeft)
                        result |= 1;
                if (compareType == topLeft)
                        result |= 2;
                if (compareType == topRight)
                        result |= 4;
                if (compareType == bottomRight)
                        result |= 8;
                return result;
        }

        public static short GetVoxelShape(int index, FillType fillType, NativeArray<FillType> fillTypes, int resolution)
        {
                int topIndex = index + resolution;
                int topRightIndex = index + resolution + 1;
                int rightIndex = index + 1;

                FillType currentFill = fillTypes[index];
                FillType topFill = GetNeightbourFillType(topIndex, fillTypes);
                FillType topRightFill = GetNeightbourFillType(topRightIndex, fillTypes);
                FillType rightFill = GetNeightbourFillType(rightIndex, fillTypes);

                return GetVoxelShape(
                        fillType,
                        currentFill,
                        topFill,
                        topRightFill,
                        rightFill);
        }

        private static FillType GetNeightbourFillType(int index, NativeArray<FillType> fillTypes)
        {
                if (index >= fillTypes.Length)
                        return FillType.None;
                return fillTypes[index];
        }

        public static FillType GetNeightbour(NativeArray<FillType> fillTypes, int index)
        {
                if (index >= fillTypes.Length)
                    return FillType.None;
                return fillTypes[index];
        }

        public static float2 GetNeightbourOffset(int index, NativeArray<float2> offsets)
        {
                if (index >= offsets.Length)
                        return float2.zero;
                return offsets[index];
        }
}
