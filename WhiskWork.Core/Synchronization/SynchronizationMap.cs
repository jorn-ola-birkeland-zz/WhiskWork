using System;
using System.Collections.Generic;

namespace WhiskWork.Core.Synchronization
{
    class MapEntry
    {
        
    }

    public class SynchronizationMap
    {
        private readonly Dictionary<ISynchronizationAgent, Dictionary<string, string>> _maps;
        private readonly Dictionary<string,string> _map;
        private readonly Dictionary<string, string> _reverseMap;

        public SynchronizationMap(ISynchronizationAgent fromAgent, ISynchronizationAgent toAgent)
        {
            _maps = new Dictionary<ISynchronizationAgent, Dictionary<string, string>>();
            _map = new Dictionary<string, string>();
            _reverseMap = new Dictionary<string, string>();

            _maps.Add(fromAgent,_map);
            _maps.Add(toAgent, _reverseMap);
        }

        public void AddReciprocalEntry(string fromStatus, string toStatus)
        {
            _map.Add(fromStatus,toStatus);
            _reverseMap.Add(toStatus,fromStatus);
        }

        public void AddForwardEntry(string fromStatus, string toStatus)
        {
            _map.Add(fromStatus, toStatus);
        }

        public void AddReverseEntry(string toStatus, string fromStatus)
        {
            _reverseMap.Add(toStatus, fromStatus);
        }

        public string GetMappedValue(ISynchronizationAgent agent, string status)
        {
            if(!_maps[agent].ContainsKey(status))
            {
                throw new ArgumentException("Could not find key " + status);
            }

            return _maps[agent][status];

        }

        public bool ContainsKey(ISynchronizationAgent agent, string status)
        {
            return _maps[agent].ContainsKey(status);
        }
    }
}