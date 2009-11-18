using System;
using System.Collections;
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

        public void AddReciprocalEntry(string from, string to)
        {
            _map.Add(from,to);
            _reverseMap.Add(to,from);
        }

        public void AddForwardEntry(string from, string to)
        {
            _map.Add(from, to);
        }

        public void AddReverseEntry(string to, string from)
        {
            _reverseMap.Add(to, from);
        }

        public string GetMappedValue(ISynchronizationAgent agent, string value)
        {
            if(!_maps[agent].ContainsKey(value))
            {
                throw new ArgumentException("Could not find value " + value);
            }

            return _maps[agent][value];

        }

        public bool ContainsKey(ISynchronizationAgent agent, string value)
        {
            return _maps[agent].ContainsKey(value);
        }

        public IEnumerable<string> GetKeys(ISynchronizationAgent agent)
        {
            return _maps[agent].Keys;
        }
    }
}