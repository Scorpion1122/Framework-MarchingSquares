using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

//[BurstCompile]
//public struct UpdateMarchingIndex : IJobParallelFor
//{
//    [ReadOnly] public int resolution;
//    [ReadOnly] public NativeArray<FillType> fillTypes;
//    [WriteOnly] public NativeArray<int> marchingIndex;
//    
//    public void Execute(int index)
//    {
//        int topIndex = index + resolution;
//        int topRightIndex = index + resolution + 1;
//        int rightIndex = index + + 1;
//        
//        FillType current = fillTypes[index];
//        FillType top = GetNeightbour(topIndex);
//        FillType topRight = GetNeightbour(topRightIndex);
//        FillType right = GetNeightbour(rightIndex);
//        
//        int voxelType = VoxelUtility.GetVoxelShape(
//            fillType, 
//            current, 
//            top, 
//            topRight,
//            right);
//    }
//    
//    private FillType GetNeightbour(int index)
//    {
//        if (index >= fillTypes.Length)
//            return FillType.None;
//        return fillTypes[index];
//    }
//}