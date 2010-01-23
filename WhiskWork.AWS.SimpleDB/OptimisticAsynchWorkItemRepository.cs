using System;
using System.Threading;
using WhiskWork.Core;
namespace WhiskWork.AWS.SimpleDB
{
    public class OptimisticAsynchWorkItemRepository : ReadPassThroughWorkItemRepositoryProxy
    {
        public OptimisticAsynchWorkItemRepository(ICacheableWorkItemRepository innerRepository) : base(innerRepository)
        {
        }

        public override void CreateWorkItem(WorkItem workItem)
        {
            RunOptimistic(()=>InnerRepository.CreateWorkItem(workItem));
        }

        public override void UpdateWorkItem(WorkItem workItem)
        {
            RunOptimistic(() => InnerRepository.UpdateWorkItem(workItem));
        }

        public override void DeleteWorkItem(string workItemId)
        {
            RunOptimistic(() => InnerRepository.DeleteWorkItem(workItemId));
        }

        private static void RunOptimistic(ThreadStart threadStart)
        {
            var thread = new Thread(threadStart); 
            thread.Start();
        }

    }
}
