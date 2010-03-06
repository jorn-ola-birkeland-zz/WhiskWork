using WhiskWork.Core;
using WhiskWork.Data.Ado;

namespace WhiskWork.CommondLineWebServer
{
    internal class AdoWorkflowRepositoryFactory: IWorkflowRepositoryFactory
    {
        private readonly string _connectionString;

        public AdoWorkflowRepositoryFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IWorkflowRepository WorkflowRepository
        {
            get
            {
                var workItemRepository = new AdoWorkItemRepository(_connectionString);
                var workStepRepository = new AdoWorkStepRepository(_connectionString);

                return new TransactionalWorkflowRepository(workItemRepository, workStepRepository);
            }
        }


    }
}