using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public static class WorkStepRepositoryExtensions
    {
        public static void DeleteWorkStepsRecursively(this IWorkStepRepository workStepRepository, WorkStep step)
        {
            foreach (var workStep in workStepRepository.GetChildWorkSteps(step.Path))
            {
                DeleteWorkStepsRecursively(workStepRepository, workStep);
            }

            workStepRepository.DeleteWorkStep(step.Path);
        }

        public static bool IsLeafStep(this IWorkStepRepository workStepRepository, WorkStep step)
        {
            return GetLeafStep(workStepRepository, step).Path == step.Path;
        }

        public static WorkStep GetLeafStep(this IWorkStepRepository workStepRepository, WorkStep workStep)
        {
            return GetLeafStep(workStepRepository, workStep.Path);
        }

        public static WorkStep GetLeafStep(this IWorkStepRepository workStepRepository, string path)
        {
            var currentWorkStep = workStepRepository.GetWorkStep(path);
            while (true)
            {
                if (currentWorkStep.Type == WorkStepType.Expand)
                {
                    break;
                }

                var subSteps = workStepRepository.GetChildWorkSteps(currentWorkStep.Path);
                if (subSteps.Count() == 0)
                {
                    break;
                }

                currentWorkStep = subSteps.OrderBy(subStep => subStep.Ordinal).ElementAt(0);
            }

            return currentWorkStep;
        }


        public static bool IsWithinExpandStep(this IWorkStepRepository workStepRepository, WorkStep workStep)
        {
            WorkStep expandStep;
            return TryLocateFirstAncestorStepOfType(workStepRepository, workStep, WorkStepType.Expand, out expandStep);
        }

        public static bool IsWithinExpandStep(this IWorkStepRepository workStepRepository, WorkStep workStep, out WorkStep expandStep)
        {
            return TryLocateFirstAncestorStepOfType(workStepRepository, workStep, WorkStepType.Expand, out expandStep);
        }


        public static bool IsWithinTransientStep(this IWorkStepRepository workStepRepository, WorkStep workStep, out WorkStep transientStep)
        {
            return TryLocateFirstAncestorStepOfType(workStepRepository, workStep, WorkStepType.Transient, out transientStep);
        }

        public static bool IsWithinParallelStep(this IWorkStepRepository workStepRepository, WorkStep workStep)
        {
            WorkStep parallelStepRoot;
            return TryLocateFirstAncestorStepOfType(workStepRepository, workStep, WorkStepType.Parallel, out parallelStepRoot);
        }


        public static bool IsWithinParallelStep(this IWorkStepRepository workStepRepository, WorkStep workStep, out WorkStep parallelStepRoot)
        {
            return TryLocateFirstAncestorStepOfType(workStepRepository, workStep, WorkStepType.Parallel, out parallelStepRoot);
        }


        public static bool IsInExpandStep(this IWorkStepRepository workStepRepository, WorkItem workItem, out WorkStep expandStep)
        {
            expandStep = null;
            bool isInExpandStep = workStepRepository.GetWorkStep(workItem.Path).Type == WorkStepType.Expand;

            if (isInExpandStep)
            {
                expandStep = workStepRepository.GetWorkStep(workItem.Path);
            }

            return isInExpandStep;
        }


        public static bool IsExpandStep(this IWorkStepRepository workStepRepository, WorkStep step)
        {
            return step.Type == WorkStepType.Expand;
        }

        public static bool IsParallelStep(this IWorkStepRepository workStepRepository, string path)
        {
            return !IsRoot(workStepRepository, path) && IsParallelStep(workStepRepository, workStepRepository.GetWorkStep(path));
        }

        public static bool IsParallelStep(this IWorkStepRepository workStepRepository, WorkStep step)
        {
            return step.Type == WorkStepType.Parallel;
        }

        public static bool IsRoot(this IWorkStepRepository workStepRepository, string path)
        {
            return path == WorkStep.Root.Path;
        }

        public static bool IsValidWorkStepForWorkItem(this IWorkStepRepository workStepRepository, WorkItem item, WorkStep workStep)
        {
            return item.Classes.Contains(workStep.WorkItemClass);
        }

        public static IEnumerable<string> GetWorkItemClasses(this IWorkStepRepository workStepRepository, WorkStep workStep)
        {
            yield return workStep.WorkItemClass;
        }


        private static bool TryLocateFirstAncestorStepOfType(this IWorkStepRepository workStepRepository, WorkStep workStep, WorkStepType stepType, out WorkStep ancestorStep)
        {
            var currentPath = workStep.Path;
            do
            {
                var currentWorkStep = workStepRepository.GetWorkStep(currentPath);
                if (currentWorkStep.Type == stepType)
                {
                    ancestorStep = currentWorkStep;
                    return true;
                }

                currentPath = currentWorkStep.ParentPath;
            }
            while (currentPath != WorkStep.Root.Path);

            ancestorStep = null;
            return false;

        }

    }
}
