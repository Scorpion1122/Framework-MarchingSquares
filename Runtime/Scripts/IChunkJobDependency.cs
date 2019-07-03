using Unity.Jobs;

namespace Thijs.Framework.MarchingSquares
{
    public interface IChunkJobDependency
    {
        JobHandle ScheduleChunkJob(VoxelGrid grid, ChunkData chunkData, JobHandle dependency);

        void OnJobCompleted();
    }
}
