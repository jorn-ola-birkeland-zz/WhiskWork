using WhiskWork.Core;

namespace WhiskWork.CommondLineWebServer
{
    internal class MemoryWorkflowRepositoryFactory : IWorkflowRepositoryFactory
    {
        public IWorkflowRepository WorkflowRepository
        {
            get
            {
                var workItemRepository = new MemoryWorkItemRepository();
                var workStepRepository = new MemoryWorkStepRepository();

                return new WorkflowRepository(workItemRepository, workStepRepository);
            }
        }
    }
}