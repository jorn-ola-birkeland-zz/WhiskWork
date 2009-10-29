using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhiskWork.Core
{
    public class LoggingWorkItemRepository : IWorkItemRepository
    {
        private readonly IWorkItemLogger _logger;
        private readonly IWorkItemRepository _repository;

        public LoggingWorkItemRepository(IWorkItemLogger logger, IWorkItemRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }


        public bool ExistsWorkItem(string id)
        {
            return _repository.ExistsWorkItem(id);
        }

        public WorkItem GetWorkItem(string id)
        {
            return _repository.GetWorkItem(id);
        }

        public void CreateWorkItem(WorkItem workItem)
        {
            _repository.CreateWorkItem(workItem);
            _logger.LogCreate(workItem);
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _repository.GetWorkItems(path);
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            var oldWorkItem = _repository.GetWorkItem(workItem.Id);

            _repository.UpdateWorkItem(workItem);
            _logger.LogUpdate(oldWorkItem, workItem);
        }

        public IEnumerable<WorkItem> GetChildWorkItems(string id)
        {
            return _repository.GetChildWorkItems(id);
        }

        public void DeleteWorkItem(WorkItem workItem)
        {
            _logger.LogDelete(workItem);
            _repository.DeleteWorkItem(workItem);
        }
    }
}
