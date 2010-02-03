using System;
using System.Collections.Generic;
using WhiskWork.Core;

namespace WhiskWork.Synchronizer
{
    internal class ReadOnlyWhiskWorkRepository : IWhiskWorkRepository
    {
        private readonly IWhiskWorkRepository _internalRepository;

        public ReadOnlyWhiskWorkRepository(IWhiskWorkRepository internalRepository)
        {
            _internalRepository = internalRepository;
        }

        public IEnumerable<WorkItem> GetWorkItems()
        {
            return _internalRepository.GetWorkItems();
        }

        public void PostWorkItem(WorkItem workItemUpdate)
        {
            Console.WriteLine("Post: "+workItemUpdate);
        }

        public void DeleteWorkItem(WorkItem workItem)
        {
            Console.WriteLine("Delete: " + workItem);
        }
    }
}