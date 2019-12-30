using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Thijs.Framework.MarchingSquares
{
    public class ChunkJobDependencyGraph
    {
        private List<IChunkJobScheduler> jobs = new List<IChunkJobScheduler>();

        // Key depends on values
        private Dictionary<Type, List<Type>> dependent = new Dictionary<Type, List<Type>>();

        // Values depend on Key
        private Dictionary<Type, List<Type>> dependee = new Dictionary<Type, List<Type>>();

        public int Count => jobs.Count;

        public IChunkJobScheduler this[int index]
        {
            get { return jobs[index]; }
        }

        public JobHandle ScheduleJobs(TileTerrain grid, ChunkData chunkData, JobHandle jobHandle)
        {
            // In principle all dependency jobs should be able to execute at the same time
            JobHandle? combinedHandle = null;
            for (int i = 0; i < jobs.Count; i++)
            {
                IChunkJobScheduler dependency = jobs[i];
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

        private void RegisterDependencies(Type type)
        {
            DependsOnAttribute[] attributes = (DependsOnAttribute[])type.GetCustomAttributes(typeof(DependsOnAttribute), true);
            for (int i = 0; i < attributes.Length; i++)
                RegisterDependency(type, attributes[i].Type);
        }

        private void RegisterDependency(Type type, Type dependsOnType)
        {
            if (!dependent.TryGetValue(type, out List<Type> dependentList))
            {
                dependentList = new List<Type>();
                dependent[type] = dependentList;
            }
            dependentList.Add(dependsOnType);

            if (!dependee.TryGetValue(dependsOnType, out List<Type> dependeeList))
            {
                dependeeList = new List<Type>();
                dependee[dependsOnType] = dependeeList;
            }
            dependeeList.Add(type);
        }

        public void OnJobsCompleted(ChunkData chunkData)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                jobs[i].OnJobCompleted(chunkData);
            }
        }

        public void Add(IChunkJobScheduler scheduler)
        {
            RegisterDependencies(scheduler.GetType());

            if (Count == 0)
            {
                jobs.Add(scheduler);
                return;
            }

            int insertAfter = InsertAfterIndex(scheduler.GetType());
            int insertBefore = InsertBeforeIndex(scheduler.GetType());
            int nonBlockingIndex = GetFirstNonBlockingIndex();

            int desiredInsertIndex = 0;
            if (insertAfter != -1)
                desiredInsertIndex = insertAfter + 1;
            else if (insertBefore != -1)
                desiredInsertIndex = insertBefore;
            else if (!scheduler.IsBlocking)
                desiredInsertIndex = nonBlockingIndex;

            if (desiredInsertIndex >= jobs.Count)
                jobs.Add(scheduler);
            else
                jobs.Insert(desiredInsertIndex, scheduler);
        }

        private int InsertBeforeIndex(Type type)
        {
            if (!dependee.TryGetValue(type, out List<Type> dependents))
                return jobs.Count;

            int result = -1;
            for (int i = 0; i < dependents.Count; i++)
            {
                int indexOf = GetFirstIndexOfSchedulerType(dependents[i]);
                if (indexOf < result || result == -1)
                    result = indexOf;
            }
            return result;
        }

        private int InsertAfterIndex(Type type)
        {
            if (!dependent.TryGetValue(type, out List<Type> dependees))
                return jobs.Count;

            int result = -1;
            for (int i = 0; i < dependees.Count; i++)
            {
                int indexOf = GetLastIndexOfSchedulerType(dependees[i]);
                if (indexOf > result || result == -1)
                    result = indexOf;
            }
            return result;
        }

        private int GetFirstIndexOfSchedulerType(Type type)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].GetType() == type)
                    return i;
            }
            return jobs.Count;
        }

        private int GetLastIndexOfSchedulerType(Type type)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (jobs[i].GetType() == type)
                    return i;
            }
            return 0;
        }

        private int GetFirstNonBlockingIndex()
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                if (!jobs[i].IsBlocking)
                    return i;
            }
            return Count;
        }

        public void Remove(IChunkJobScheduler scheduler)
        {
            jobs.Remove(scheduler);
        }
    }
}
