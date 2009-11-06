using System;
using System.Collections.Generic;

namespace WhiskWork.Core.Synchronization
{
    public interface ISynchronizationAgent
    {
        IEnumerable<SynchronizationEntry> GetAll();
        void Create(SynchronizationEntry entry);
        void Delete(SynchronizationEntry entry);
        void UpdateStatus(SynchronizationEntry entry);
        void UpdateProperties(SynchronizationEntry entry);
    }
}