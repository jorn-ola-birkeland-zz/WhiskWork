using WhiskWork.AWS.SimpleDB;
using WhiskWork.Core;

namespace WhiskWork.CommondLineWebServer
{
    internal class SimpleDbWorkflowRepositoryFactory : IWorkflowRepositoryFactory
    {
        private readonly string _domainPrefix;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretAccessKey;

        public SimpleDbWorkflowRepositoryFactory(string domainPrefix, string awsAccessKey, string awsSecretAccessKey)
        {
            _domainPrefix = domainPrefix;
            _awsAccessKey = awsAccessKey;
            _awsSecretAccessKey = awsSecretAccessKey;
        }

        public IWorkflowRepository WorkflowRepository
        {
            get
            {
                var workItemRepository = new CachingWorkItemRepository(new OptimisticAsynchWorkItemRepository(new SimpleDBWorkItemRepository(_domainPrefix + "_items", _awsAccessKey, _awsSecretAccessKey)));
                var workStepRepository = new CachingWorkStepRepository(new SimpleDBWorkStepRepository(_domainPrefix + "_steps", _awsAccessKey, _awsSecretAccessKey));

                return new WorkflowRepository(workItemRepository,workStepRepository);
            }
        }
    }
}