using Unity.Jobs;

namespace Thijs.Framework.MarchingSquares
{
    public interface IChunkJobDependency
    {
        bool IsBlocking { get; }

        JobHandle ScheduleChunkJob(TileTerrain grid, ChunkData chunkData, JobHandle dependency);

        void OnJobCompleted(ChunkData chunkData);
    }
}
