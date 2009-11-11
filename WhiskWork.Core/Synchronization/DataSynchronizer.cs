using System.Linq;

namespace WhiskWork.Core.Synchronization
{
    public class DataSynchronizer
    {
        private readonly ISynchronizationAgent _master;
        private readonly ISynchronizationAgent _slave;

        public DataSynchronizer(ISynchronizationAgent master, ISynchronizationAgent slave)
        {
            _master = master;
            _slave = slave;
        }

        public void Synchronize()
        {
            var masterEntries = _master.GetAll().ToDictionary(e => e.Id);
            var slaveEntries = _slave.GetAll().ToDictionary(e => e.Id);

            foreach (var masterId in masterEntries.Keys)
            {
                if (slaveEntries.ContainsKey(masterId))
                {
                    var slaveEntry = slaveEntries[masterId];

                    var updateEntry = new SynchronizationEntry(masterId, slaveEntry.Status,
                                                               masterEntries[masterId].Properties)
                                          {Ordinal = masterEntries[masterId].Ordinal};

                    _slave.UpdateData(updateEntry);
                }
            }

        }
    }
}