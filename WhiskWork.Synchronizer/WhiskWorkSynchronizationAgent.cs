using System;
using System.Collections.Generic;
using WhiskWork.Core.Synchronization;
using WhiskWork.Core;
using System.Linq;

namespace WhiskWork.Synchronizer
{
    class WhiskWorkSynchronizationAgent : ISynchronizationAgent
    {
        private readonly IWhiskWorkRepository _repository;
        private readonly WorkStep _beginStep;
        private readonly Converter<IEnumerable<WorkItem>, IEnumerable<SynchronizationEntry>> _mapper;


        public WhiskWorkSynchronizationAgent(IWhiskWorkRepository repository, Converter<IEnumerable<WorkItem>,IEnumerable<SynchronizationEntry>> mapper, string beginStepPath)
        {
            _repository = repository;
            _beginStep = WorkStep.New(beginStepPath);
            _mapper = mapper;
        }

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            return _mapper(_repository.GetWorkItems());
        }

        public void Create(SynchronizationEntry entry)
        {
            var workItem = CreateWorkItem(entry);

            _repository.PostWorkItem(workItem.MoveTo(_beginStep));

            _repository.PostWorkItem(workItem);
        }

        public void Delete(SynchronizationEntry entry)
        {
            var workItem = WorkItem.New(entry.Id,entry.Status);

            _repository.DeleteWorkItem(workItem);
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            var workItem = WorkItem.New(entry.Id, entry.Status);

            _repository.PostWorkItem(workItem);
        }

        public void UpdateData(SynchronizationEntry entry)
        {
            var workItem = CreateWorkItem(entry);

            _repository.PostWorkItem(workItem);
        }

        private static WorkItem CreateWorkItem(SynchronizationEntry entry)
        {
            var workItem = WorkItem.New(entry.Id, entry.Status);

            if(entry.Ordinal.HasValue)
            {
                workItem = workItem.UpdateOrdinal(entry.Ordinal.Value);
            }

            return workItem.UpdateProperties(entry.Properties);
        }
    }
}