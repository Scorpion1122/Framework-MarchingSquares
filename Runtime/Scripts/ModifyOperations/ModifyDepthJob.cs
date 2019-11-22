using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Thijs.Framework.MarchingSquares
{
    [BurstCompile]
    public struct ModifyDepthJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<FillType> fillTypes;
        [ReadOnly] public NativeArray<float2> offsets;
        [WriteOnly] public NativeArray<float> depth;

        public void Execute(int index)
        {
        }
    }
}
