using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class WorkStepQuery
    {
        private readonly IWorkflowRepository _workflowRepository;
        public WorkStepQuery(IWorkflowRepository repository)
        {
            _workflowRepository = repository;
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
            var currentWorkStep = _workflowRepository.GetWorkStep(path);
            while (true)
            {
                if (currentWorkStep.Type == WorkStepType.Expand)
                {
                    break;
                }

                var subSteps = _workflowRepository.GetChildWorkSteps(currentWorkStep.Path);
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
            bool isInTransientStep = _workflowRepository.GetWorkStep(workItem.Path).Type == WorkStepType.Transient;

            if(isInTransientStep)
            {
                transientStep = _workflowRepository.GetWorkStep(workItem.Path);
            }
            
            return isInTransientStep;
        }

        public bool IsExpandStep(WorkStep step)
        {
            return step.Type == WorkStepType.Expand;
        }

        public bool IsParallelStep(string path)
        {
            return !IsRoot(path) && IsParallelStep(_workflowRepository.GetWorkStep(path));
        }

        public bool IsParallelStep(WorkStep step)
        {
            return step.Type == WorkStepType.Parallel;
        }

        public bool IsRoot(string path)
        {
            return path == "/";
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
                var currentWorkStep = _workflowRepository.GetWorkStep(currentPath);
                if (currentWorkStep.Type == stepType)
                {
                    ancestorStep = currentWorkStep;
                    return true;
                }

                currentPath = currentWorkStep.ParentPath;
            }
            while (currentPath != "/");

            ancestorStep = null;
            return false;

        }


    }
}