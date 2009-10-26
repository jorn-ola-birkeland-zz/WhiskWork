using System;
using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class AdvancedWorkflowRepository : IWorkflowRepository
    {
        private readonly IWorkflowRepository _workflowRepository;

        public AdvancedWorkflowRepository(IWorkflowRepository workflowRepository)
        {
            _workflowRepository = workflowRepository;
        }

        public void DeleteWorkStepsRecursively(WorkStep step)
        {
            foreach (var workStep in _workflowRepository.GetChildWorkSteps(step.Path))
            {
                DeleteWorkStepsRecursively(workStep);
            }

            DeleteWorkStep(step.Path);
        }


        public void CreateWorkStep(WorkStep workStep)
        {
            _workflowRepository.CreateWorkStep(workStep);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            return _workflowRepository.GetChildWorkSteps(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workflowRepository.GetWorkStep(path);
        }

        public void DeleteWorkStep(string path)
        {
            _workflowRepository.DeleteWorkStep(path);
        }
    }
}
