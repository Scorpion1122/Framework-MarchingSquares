using System;
using Unity.Collections;

namespace Thijs.Framework.MarchingSquares
{
    public struct VertexCache : IDisposable
    {
        /// <summary>
        /// * - * - * nextRow
        /// * - - - * midRow
        /// o - * - * prevRow
        /// o is source of square
        /// </summary>
        public NativeArray<int> prevRow;
        public NativeArray<int> midRow;
        public NativeArray<int> nextRow;

        public VertexCache(int resolution)
        {
            // prev and cur also needs to store the vertex between voxels, thats wy resolution * 2
            prevRow = new NativeArray<int>(resolution * 2, Allocator.Temp);
            midRow = new NativeArray<int>(resolution, Allocator.Temp);
            nextRow = new NativeArray<int>(resolution * 2, Allocator.Temp);
        }

        public void Swap()
        {
            NativeArray<int> temp = prevRow;
            prevRow = nextRow;
            nextRow = temp;
        }

        public void Dispose()
        {
            prevRow.Dispose();
            midRow.Dispose();
            nextRow.Dispose();
        }
    }
}