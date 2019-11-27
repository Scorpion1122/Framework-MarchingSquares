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
        public int resolution;

        public bool IsCreated => midRow.IsCreated;

        public VertexCache(int resolution)
        {
            // prev and cur also needs to store the vertex between voxels, thats wy resolution * 2
            prevRow = new NativeArray<int>(resolution * 2, Allocator.Persistent);
            midRow = new NativeArray<int>(resolution, Allocator.Persistent);
            nextRow = new NativeArray<int>(resolution * 2, Allocator.Persistent);
            this.resolution = resolution;
        }

        public void Swap()
        {
            NativeArray<int> temp = prevRow;
            prevRow = nextRow;
            nextRow = temp;
        }

        public void Dispose()
        {
            if (!IsCreated)
                return;
            
            prevRow.Dispose();
            midRow.Dispose();
            nextRow.Dispose();
        }
    }
}