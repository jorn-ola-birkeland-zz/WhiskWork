using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class WorkStepQuery
    {
        private readonly IWorkStepRepository _workStepRepository;
        public WorkStepQuery(IWorkStepRepository repository)
        {
            _workStepRepository = repository;
        }

        public bool IsLeafStep(WorkStep step)
        {
            return GetLeafStep(step).Path == step.Path;
        }

        public WorkStep GetLeafStep(WorkStep workStep)
        {
            return GetLeafStep(workStep.Path);
        }

        public WorkStep GetLeafStep(string path)
        {
            var currentWorkStep = _workStepRepository.GetWorkStep(path);
            while (true)
            {
                if (currentWorkStep.Type == WorkStepType.Expand)
                {
                    break;
                }

                var subSteps = _workStepRepository.GetChildWorkSteps(currentWorkStep.Path);
                if (subSteps.Count() == 0)
                {
                    break;
                }

                currentWorkStep = subSteps.OrderBy(subStep => subStep.Ordinal).ElementAt(0);
            }

            return currentWorkStep;
        }


        public bool IsWithinExpandStep(WorkStep workStep)
        {
            WorkStep expandStep;
            return TryLocateFirstAncestorStepOfType(workStep, WorkStepType.Expand, out expandStep);
        }

        public bool IsWithinTransientStep(WorkStep workStep, out WorkStep transientStep)
        {
            return TryLocateFirstAncestorStepOfType(workStep, WorkStepType.Transient, out transientStep);
        }

        public bool IsWithinParallelStep(WorkStep workStep, out WorkStep parallelStepRoot)
        {
            return TryLocateFirstAncestorStepOfType(workStep, WorkStepType.Parallel, out parallelStepRoot);
        }

        public bool IsInTransientStep(WorkItem workItem, out WorkStep transientStep)
        {
            transientStep = null;
            bool isInTransientStep = _workStepRepository.GetWorkStep(workItem.Path).Type == WorkStepType.Transient;

            if(isInTransientStep)
            {
                transientStep = _workStepRepository.GetWorkStep(workItem.Path);
            }
            
            return isInTransientStep;
        }

        public bool IsExpandStep(WorkStep step)
        {
            return step.Type == WorkStepType.Expand;
        }

        public bool IsParallelStep(string path)
        {
            return !IsRoot(path) && IsParallelStep(_workStepRepository.GetWorkStep(path));
        }

        public bool IsParallelStep(WorkStep step)
        {
            return step.Type == WorkStepType.Parallel;
        }

        public bool IsRoot(string path)
        {
            return path == WorkStep.Root.Path;
        }

        public bool IsValidWorkStepForWorkItem(WorkItem item, WorkStep workStep)
        {
            return item.Classes.Contains(workStep.WorkItemClass);
        }

        public IEnumerable<string> GetWorkItemClasses(WorkStep workStep)
        {
            yield return workStep.WorkItemClass;
        }

        
        private bool TryLocateFirstAncestorStepOfType(WorkStep workStep, WorkStepType stepType, out WorkStep ancestorStep)
        {
            var currentPath = workStep.Path;
            do
            {
                var currentWorkStep = _workStepRepository.GetWorkStep(currentPath);
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