#region

using System;
using System.Collections.Generic;
using System.Linq;
using WhiskWork.Generic;

#endregion

namespace WhiskWork.Core.Synchronization
{
    public class DataSynchronizer
    {
        private readonly ISynchronizationAgent _master;
        private readonly ISynchronizationAgent _slave;
        private readonly SynchronizationMap _propertyMap;

        public DataSynchronizer(SynchronizationMap propertyMap,  ISynchronizationAgent master, ISynchronizationAgent slave)
        {
            _propertyMap = propertyMap;
            _master = master;
            _slave = slave;
        }

        public void Synchronize()
        {
            var masterEntries = _master.GetAll().ToDictionary(e => e.Id);
            var slaveEntries = _slave.GetAll().ToDictionary(e => e.Id);

            foreach (var masterId in masterEntries.Keys)
            {
                if (!slaveEntries.ContainsKey(masterId))
                {
                    continue;
                }

                var slaveEntry = slaveEntries[masterId];
                var masterEntry = masterEntries[masterId];

                var slaveProperties = FilterForSynchronization(_slave, slaveEntry.Properties);
                var slaveMappedMasterProperties = FilterForSynchronization(_slave, MapProperties(_master, masterEntry.Properties));

                SynchronizationEntry updateEntry = null;

                if (!AreEqual(slaveMappedMasterProperties, slaveProperties))
                {
                    Console.WriteLine("Properties differ");
                    Console.WriteLine("Slavemapped master:"+ slaveMappedMasterProperties.Select(kv => kv.Value).Join(','));
                    Console.WriteLine("Slave             :" + slaveProperties.Select(kv => kv.Value).Join(','));


                    updateEntry = new SynchronizationEntry(masterId, slaveEntry.Status, slaveMappedMasterProperties) {Ordinal = slaveEntry.Ordinal};
                }

                else if (SynchronizeOrdinal && masterEntry.Ordinal != slaveEntry.Ordinal)
                {
                    Console.WriteLine("Ordinals differ: {0}-{1}",masterEntry,slaveEntry);
                    updateEntry = new SynchronizationEntry(masterId, slaveEntry.Status, slaveMappedMasterProperties)
                                      {Ordinal = masterEntry.Ordinal};
                }

                if (updateEntry != null)
                {
                    _slave.UpdateData(updateEntry);
                }
            }
        }

        public bool SynchronizeOrdinal
        {
            get;
            set;
        }

        private Dictionary<string,string> MapProperties(ISynchronizationAgent agent, Dictionary<string,string> properties)
        {
            var mappedProperties = new Dictionary<string, string>();

            foreach (var keyValue in properties)
            {
                if(_propertyMap.ContainsKey(agent,keyValue.Key))
                {
                    mappedProperties.Add(_propertyMap.GetMappedValue(agent, keyValue.Key),keyValue.Value);     
                }
            }

            return mappedProperties;
        }

        private static bool AreEqual(Dictionary<string, string> properties1, IDictionary<string, string> properties2)
        {
            if (properties1.Count != properties2.Count)
            {
                Console.WriteLine("Properties length '{0}'-'{1}'", properties1.Count, properties2.Count);
                return false;
            }

            foreach (var key in properties1.Keys)
            {
                if (!properties2.ContainsKey(key))
                {
                    return false;
                }

                if (properties1[key] != properties2[key])
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<string, string> FilterForSynchronization(ISynchronizationAgent agent, IDictionary<string, string> properties)
        {
            var filteredProperties = new Dictionary<string, string>();

            foreach (var propertyKey in _propertyMap.GetKeys(agent))
            {
                if (properties.ContainsKey(propertyKey))
                {
                    filteredProperties.Add(propertyKey, properties[propertyKey]);
                }
            }

            return filteredProperties;
        }

    }
}