using System;
using System.Linq;

namespace WhiskWork.Core.Synchronization
{
    public class CreationSynchronizer
    {
        private readonly ISynchronizationAgent _master;
        private readonly ISynchronizationAgent _slave;
        private readonly SynchronizationMap _map;

        public CreationSynchronizer(SynchronizationMap map, ISynchronizationAgent master, ISynchronizationAgent slave)
        {
            _map = map;
            _master = master;
            _slave = slave;
        }

        public void Synchronize()
        {
            var masterEntries = _master.GetAll();
            var slaveEntries = _slave.GetAll();

            var idsForDeletion = slaveEntries.Select(e=>e.Id).Except(masterEntries.Select(e=>e.Id)); 
            foreach (var id in idsForDeletion)
            {
                var entry = slaveEntries.Where(e=>e.Id==id).Single();
                Console.WriteLine("Delete:"+entry);
                _slave.Delete(entry);
            }

            var idsForCreation = masterEntries.Select(e => e.Id).Except(slaveEntries.Select(e => e.Id));
            foreach (var id in idsForCreation)
            {
                var entry = masterEntries.Where(e => e.Id == id).Single();
                Console.WriteLine("Create:"+entry);

                SynchronizationEntry slaveEntry;

                if (TryToSlaveEntry(entry, out slaveEntry))
                {
                    _slave.Create(slaveEntry);
                }
            }
        }

        private bool TryToSlaveEntry(SynchronizationEntry masterEntry, out SynchronizationEntry slaveEntry)
        {
            slaveEntry = null;

            if(!_map.ContainsKey(_master, masterEntry.Status))
            {
                return false;
            }

            var slaveStatus = _map.GetMappedValue(_master, masterEntry.Status);

            slaveEntry = new SynchronizationEntry(masterEntry.Id, slaveStatus, masterEntry.Properties);

            return true;
        }

        //private SynchronizationEntry ToMasterEntry(SynchronizationEntry slaveEntry)
        //{
        //    var masterStatus = _map.GetMappedValue(_master, slaveEntry.Status);

        //    return new SynchronizationEntry(slaveEntry.Id, masterStatus, slaveEntry.Properties);
            
        //}
    }
}