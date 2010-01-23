using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public static class WriteableWorkflowRepositoryExtensions
    {
        public static void DeleteWorkStepsRecursively(this IWorkflowRepository repository, WorkStep step)
        {
            foreach (var workStep in repository.GetChildWorkSteps(step.Path))
            {
                DeleteWorkStepsRecursively(repository, workStep);
            }

            repository.DeleteWorkStep(step.Path);
        }
        
    }

    public static class WorkflowRepositoryExtensions
    {

        public static bool IsLeafStep(this IReadableWorkflowRepository repository, WorkStep step)
        {
            return GetLeafStep(repository, step).Path == step.Path;
        }

        public static WorkStep GetLeafStep(this IReadableWorkflowRepository repository, WorkStep workStep)
        {
            return GetLeafStep(repository, workStep.Path);
        }

        public static WorkStep GetLeafStep(this IReadableWorkflowRepository repository, string path)
        {
            var currentWorkStep = repository.GetWorkStep(path);
            while (true)
            {
                if (currentWorkStep.Type == WorkStepType.Expand)
                {
                    break;
                }

                var subSteps = repository.GetChildWorkSteps(currentWorkStep.Path);
                if (subSteps.Count() == 0)
                {
                    break;
                }

                currentWorkStep = subSteps.OrderBy(subStep => subStep.Ordinal).ElementAt(0);
            }

            return currentWorkStep;
        }


        public static bool IsWithinExpandStep(this IReadableWorkflowRepository repository, WorkStep workStep)
        {
            WorkStep expandStep;
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Expand, out expandStep);
        }

        public static bool IsWithinExpandStep(this IReadableWorkflowRepository repository, WorkStep workStep, out WorkStep expandStep)
        {
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Expand, out expandStep);
        }

        public static bool IsWithinTransientStep(this IReadableWorkflowRepository repository, WorkStep workStep)
        {
            WorkStep transientStep;
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Transient, out transientStep);
        }


        public static bool IsWithinTransientStep(this IReadableWorkflowRepository repository, WorkStep workStep, out WorkStep transientStep)
        {
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Transient, out transientStep);
        }

        public static bool IsWithinParallelStep(this IReadableWorkflowRepository repository, WorkStep workStep)
        {
            WorkStep parallelStepRoot;
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Parallel, out parallelStepRoot);
        }


        public static bool IsWithinParallelStep(this IReadableWorkflowRepository repository, WorkStep workStep, out WorkStep parallelStepRoot)
        {
            return TryLocateFirstAncestorStepOfType(repository, workStep, WorkStepType.Parallel, out parallelStepRoot);
        }

        public static bool IsInExpandStep(this IReadableWorkflowRepository workflow, WorkItem workItem)
        {
            WorkStep expandStep;

            return IsInExpandStep(workflow, workItem, out expandStep);
        }


        public static bool IsInExpandStep(this IReadableWorkflowRepository repository, WorkItem workItem, out WorkStep expandStep)
        {
            expandStep = null;
            bool isInExpandStep = repository.GetWorkStep(workItem.Path).Type == WorkStepType.Expand;

            if (isInExpandStep)
            {
                expandStep = repository.GetWorkStep(workItem.Path);
            }

            return isInExpandStep;
        }


        public static bool IsExpandStep(this IReadableWorkflowRepository repository, WorkStep step)
        {
            return step.Type == WorkStepType.Expand;
        }

        public static bool IsParallelStep(this IReadableWorkflowRepository repository, string path)
        {
            return !IsRoot(repository, path) && IsParallelStep(repository, repository.GetWorkStep(path));
        }

        public static bool IsParallelStep(this IReadableWorkflowRepository repository, WorkStep step)
        {
            return step.Type == WorkStepType.Parallel;
        }

        public static bool IsRoot(this IReadableWorkflowRepository repository, string path)
        {
            return path == WorkStep.Root.Path;
        }

        public static bool IsValidWorkStepForWorkItem(this IReadableWorkflowRepository repository, WorkItem item, WorkStep workStep)
        {
            return item.Classes.Contains(workStep.WorkItemClass);
        }

        public static IEnumerable<string> GetWorkItemClasses(this IReadableWorkflowRepository repository, WorkStep workStep)
        {
            yield return workStep.WorkItemClass;
        }


        private static bool TryLocateFirstAncestorStepOfType(this IReadableWorkStepRepository repository, WorkStep workStep, WorkStepType stepType, out WorkStep ancestorStep)
        {
            var currentPath = workStep.Path;
            do
            {
                var currentWorkStep = repository.GetWorkStep(currentPath);
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

        public static bool TryLocateWorkItem(this IReadableWorkflowRepository repository, string id, out WorkItem item)
        {
            item = repository.GetWorkItem(id);

            return item != null;
        }


        public static bool IsExpandLockedWorkItem(this IReadableWorkflowRepository repository, WorkItem item)
        {
            return item.Status == WorkItemStatus.ExpandLocked;
        }

        public static bool IsChildOfParallelledWorkItem(this IReadableWorkflowRepository repository, WorkItem workItem)
        {
            WorkItem parent;
            return IsChildOfParallelledWorkItem(repository, workItem, out parent);
        }

        public static bool IsChildOfParallelledWorkItem(this IReadableWorkflowRepository repository, WorkItem workItem, out WorkItem parent)
        {
            if (workItem.Parent != null)
            {
                parent = repository.GetWorkItem(workItem.Parent.Id);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    return true;
                }
            }

            parent = null;
            return false;
        }

        public static bool IsMergeableParallelledChild(this IReadableWorkflowRepository repository, WorkItem item, WorkStep toStep)
        {
            if (!repository.IsChildOfParallelledWorkItem(item))
            {
                return false;
            }

            var isMergeable = true;
            foreach (var childWorkItem in repository.GetChildWorkItems(item.Parent).Where(wi => wi.Id != item.Id))
            {
                isMergeable &= childWorkItem.Path == toStep.Path;
            }
            return isMergeable;
        }



        public static bool IsParallelLockedWorkItem(this IReadableWorkflowRepository repository, WorkItem workItem)
        {
            return workItem.Status == WorkItemStatus.ParallelLocked;
        }

        public static int GetNextOrdinal(this IReadableWorkflowRepository repository, WorkItem workItem)
        {
            var workItemsInStep = repository.GetWorkItems(workItem.Path);

            return workItemsInStep.Count() > 0 ? workItemsInStep.Max(wi => wi.Ordinal.HasValue ? wi.Ordinal.Value : 0) + 1 : 1;
        }

        public static bool IsChildOfExpandedWorkItem(this IReadableWorkflowRepository workflowRepository, WorkItem item)
        {
            if (item.Parent == null)
            {
                return false;
            }

            var parent = workflowRepository.GetWorkItem(item.Parent.Id);
            var workStep = workflowRepository.GetWorkStep(parent.Path);

            return workStep.Type == WorkStepType.Expand;
        }



        public static bool IsDone(this IReadableWorkflowRepository workflowRepository, WorkItem item)
        {
            return workflowRepository.GetWorkStep(item.Path).Type == WorkStepType.End;
        }


        public static IEnumerable<WorkItem> GetWorkItemsRecursively(this IReadableWorkflowRepository workflowRepository, WorkStep workStep)
        {
            foreach (var childWorkStep in workflowRepository.GetChildWorkSteps(workStep.Path))
            {
                foreach (var workItem in GetWorkItemsRecursively(workflowRepository, childWorkStep))
                {
                    yield return workItem;
                }
            }

            foreach (var workItem in workflowRepository.GetWorkItems(workStep.Path))
            {
                yield return workItem;
            }
        }
    }
}
