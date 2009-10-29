using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class ParallelStepHelper
    {
        private readonly IWorkflowRepository _workflowRepository;

        public ParallelStepHelper(IWorkflowRepository repository)
        {
            _workflowRepository = repository;
        }
        public static string GetParallelId(string id, WorkStep parallelRootStep, WorkStep toStep)
        {
            return id+"."+toStep.Path.Remove(0, parallelRootStep.Path.Length+1);
        }

        public IEnumerable<WorkItem> SplitForParallelism(WorkItem parentItem, WorkStep parallelRootStep)
        {
            var childWorkItems = new List<WorkItem>();

            foreach (var subStep in _workflowRepository.GetChildWorkSteps(parallelRootStep.Path))
            {
                var childId = GetParallelId(parentItem.Id, parallelRootStep, subStep);
                var childWorkItem = parentItem.CreateChildItem(childId).AddClass(subStep.WorkItemClass);


                childWorkItems.Add(childWorkItem);
            }

            return childWorkItems;
        }

    }
}