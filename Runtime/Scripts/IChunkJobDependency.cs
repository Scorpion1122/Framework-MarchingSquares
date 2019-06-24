using Unity.Jobs;

public interface IChunkJobDependency
{
    JobHandle ScheduleChunkJob(VoxelGrid grid, ChunkData chunkData, JobHandle dependency);

    void OnJobCompleted();
}
