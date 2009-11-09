using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core.Synchronization
{
    public class CachingSynchronizationAgent : ISynchronizationAgent
    {
        private readonly ISynchronizationAgent _innerAgent;
        private IList<SynchronizationEntry> _getAllCache;

        public CachingSynchronizationAgent(ISynchronizationAgent agent)
        {
            _innerAgent = agent;
        }

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            if(_getAllCache==null)
            {
                _getAllCache = _innerAgent.GetAll().ToList();
            }

            return _getAllCache;
        }

        public void Create(SynchronizationEntry entry)
        {
            _innerAgent.Create(entry);
            if(_getAllCache!=null)
            {
                _getAllCache.Add(entry);
            }
        }

        public void Delete(SynchronizationEntry entry)
        {
            _innerAgent.Delete(entry);
            if (_getAllCache != null)
            {
                _getAllCache.Remove(entry);
            }
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            _innerAgent.UpdateStatus(entry);
            var index = IndexOf(entry.Id);
            _getAllCache[index] = entry;
        }

        public void UpdateProperties(SynchronizationEntry entry)
        {
            _innerAgent.UpdateProperties(entry);
            var index = IndexOf(entry.Id);
            _getAllCache[index] = entry;
        }

        private int IndexOf(string id)
        {
            for (var i = 0; i < _getAllCache.Count;i++ )
            {
                if(_getAllCache[i].Id==id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}