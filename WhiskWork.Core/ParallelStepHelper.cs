using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class ParallelStepHelper
    {
        private readonly IWorkStepRepository _workStepRepository;

        public ParallelStepHelper(IWorkStepRepository repository)
        {
            _workStepRepository = repository;
        }
        public static string GetParallelId(string id, WorkStep parallelRootStep, WorkStep toStep)
        {
            return id+"-"+toStep.Path.Remove(0, parallelRootStep.Path.Length+1);
        }

        public IEnumerable<WorkItem> SplitForParallelism(WorkItem parentItem, WorkStep parallelRootStep)
        {
            var childWorkItems = new List<WorkItem>();

            foreach (var subStep in _workStepRepository.GetChildWorkSteps(parallelRootStep.Path))
            {
                var childId = GetParallelId(parentItem.Id, parallelRootStep, subStep);
                var childWorkItem = parentItem.CreateChildItem(childId).AddClass(subStep.WorkItemClass);


                childWorkItems.Add(childWorkItem);
            }

            return childWorkItems;
        }

    }
}