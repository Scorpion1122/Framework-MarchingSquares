using System.Collections.Generic;
using Unity.Jobs;

namespace Thijs.Framework.MarchingSquares
{
    public class ChunkJobDependencyGraph
    {
        private List<IChunkJobDependency> dependencies = new List<IChunkJobDependency>();

        public int Count => dependencies.Count;

        public IChunkJobDependency this[int index]
        {
            get { return dependencies[index]; }
        }

        public JobHandle ScheduleJobs(TileTerrain grid, ChunkData chunkData, JobHandle jobHandle)
        {
            // In principle all dependency jobs should be able to execute at the same time
            JobHandle? combinedHandle = null;
            for (int i = 0; i < dependencies.Count; i++)
            {
                IChunkJobDependency dependency = dependencies[i];
                JobHandle dependencyHandle = dependency.ScheduleChunkJob(grid, chunkData, jobHandle);
                if (i == 0 || dependency.IsBlocking)
                    jobHandle = dependencyHandle;
                else if (combinedHandle == null)
                    combinedHandle = dependencyHandle;
                else
                    combinedHandle = JobHandle.CombineDependencies(dependencyHandle, combinedHandle.Value);
            }

            if (combinedHandle != null)
                return combinedHandle.Value;

            return jobHandle;
        }

        public void OnJobsCompleted(ChunkData chunkData)
        {
            for (int i = dependencies.Count - 1; i >= 0; i--)
            {
                dependencies[i].OnJobCompleted(chunkData);
            }
        }

        public void Add(IChunkJobDependency dependency)
        {
            int nonBlockingIndex = GetFirstNonBlockingIndex();
            if (Count == nonBlockingIndex || !dependency.IsBlocking)
                dependencies.Add(dependency);
            else
                dependencies.Insert(nonBlockingIndex, dependency);
        }

        private int GetFirstNonBlockingIndex()
        {
            for (int i = 0; i < dependencies.Count; i++)
            {
                if (!dependencies[i].IsBlocking)
                    return i;
            }
            return Count;
        }

        public void Remove(IChunkJobDependency dependency)
        {
            dependencies.Remove(dependency);
        }
    }
}
