using System;
using System.Linq;

namespace WhiskWork.Core.Synchronization
{
    public class StatusSynchronizer
    {
        private readonly ISynchronizationAgent _master;
        private readonly ISynchronizationAgent _slave;
        private readonly SynchronizationMap _map;

        public StatusSynchronizer(SynchronizationMap map, ISynchronizationAgent master, ISynchronizationAgent slave)
        {
            _map = map;
            _master = master;
            _slave = slave;
        }

        public void Synchronize()
        {
            var masterEntries = _master.GetAll().ToDictionary(e=>e.Id);
            var masterMappedslaveEntries = _slave.GetAll().Select(e=>ToMasterEntry(e)).ToDictionary(e=>e.Id);

            //throw new NotImplementedException(masterEntries["1"]+"-"+masterMappedslaveEntries["1"]);

            foreach(var masterId in masterEntries.Keys)
            {
                if(masterMappedslaveEntries.ContainsKey(masterId))
                {
                    if (masterMappedslaveEntries[masterId].Status != masterEntries[masterId].Status)
                    {
                        //throw new NotImplementedException(ToSlaveEntry(masterEntries[masterId]).ToString());
                        _slave.UpdateStatus(ToSlaveEntry(masterEntries[masterId]));
                    }
                }
            }

        }

        private SynchronizationEntry ToSlaveEntry(SynchronizationEntry masterEntry)
        {
            var slaveStatus = _map.GetMappedValue(_master, masterEntry.Status);

            return new SynchronizationEntry(masterEntry.Id, slaveStatus, masterEntry.Properties);
        }

        private SynchronizationEntry ToMasterEntry(SynchronizationEntry slaveEntry)
        {
            var masterStatus = _map.GetMappedValue(_slave, slaveEntry.Status);

            return new SynchronizationEntry(slaveEntry.Id, masterStatus, slaveEntry.Properties);
            
        }
        
    }

    public class PropertySynchronizer
    {
        private readonly ISynchronizationAgent _master;
        private readonly ISynchronizationAgent _slave;
        private readonly SynchronizationMap _map;

        public PropertySynchronizer(SynchronizationMap map, ISynchronizationAgent master, ISynchronizationAgent slave)
        {
            _map = map;
            _master = master;
            _slave = slave;
        }

        public void Synchronize()
        {
            var masterEntries = _master.GetAll().ToDictionary(e => e.Id);
            var masterMappedslaveEntries = _slave.GetAll().Select(e => ToMasterEntry(e)).ToDictionary(e => e.Id);

            foreach (var masterId in masterEntries.Keys)
            {
                if (masterMappedslaveEntries.ContainsKey(masterId))
                {
                    _slave.UpdateProperties(ToSlaveEntry(masterEntries[masterId]));
                }
            }

        }

        private SynchronizationEntry ToSlaveEntry(SynchronizationEntry masterEntry)
        {
            var slaveStatus = _map.GetMappedValue(_master, masterEntry.Status);

            return new SynchronizationEntry(masterEntry.Id, slaveStatus, masterEntry.Properties);
        }

        private SynchronizationEntry ToMasterEntry(SynchronizationEntry slaveEntry)
        {
            var masterStatus = _map.GetMappedValue(_slave, slaveEntry.Status);

            return new SynchronizationEntry(slaveEntry.Id, masterStatus, slaveEntry.Properties);

        }

    }

}