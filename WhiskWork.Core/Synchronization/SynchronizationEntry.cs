using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhiskWork.Core.Synchronization
{
    public class SynchronizationEntry
    {
        private readonly string _id;
        private readonly string _status;
        private readonly Dictionary<string, string> _properties;


        public SynchronizationEntry(string id, string status, Dictionary<string,string> properties)
        {
            _id = id;
            _status = status;
            _properties = properties;
        }

        public string Id
        {
            get { return _id; }
        }

        public string Status
        {
            get { return _status; }
        }

        public int? Ordinal
        {
            get; set;
        }

        public Dictionary<string, string> Properties
        {
            get { return new Dictionary<string, string>(_properties); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SynchronizationEntry))
            {
                return false;
            }

            var entry = (SynchronizationEntry)obj;

            var result = true;

            result &= _id == entry._id;
            result &= _status == entry._status;
            result &= _properties.SequenceEqual(entry._properties);
            result &= Ordinal == entry.Ordinal;

            return result;
        }

        public override int GetHashCode()
        {
            var hc = _id != null ? _id.GetHashCode() : 1;
            hc ^= _status != null ? _status.GetHashCode() : 2;
            hc ^= _properties.Count > 0 ? _properties.Select(kv => kv.Key.GetHashCode() ^ kv.Value.GetHashCode()).Aggregate((hash, next) => hash ^ next) : 4;
            hc ^= Ordinal.HasValue ? Ordinal.Value.GetHashCode() : 8;

            return hc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Id={0},", _id);
            sb.AppendFormat("Status={0},", _status);
            sb.AppendFormat("Properties={0},", _properties.Count() > 0 ? _properties.Select(kv => kv.Key + ":" + kv.Value).Aggregate((current, next) => current + "&" + next) : string.Empty);
            sb.AppendFormat("Ordinal={0}", Ordinal.HasValue ? Ordinal.Value.ToString() : "<undefined>");

            return sb.ToString();
        }

        public static SynchronizationEntry FromWorkItem(WorkItem workItem)
        {
            return new SynchronizationEntry(workItem.Id, workItem.Path, ToDictionary(workItem.Properties)) {Ordinal = workItem.Ordinal};
        }

        private static Dictionary<string, string> ToDictionary(IEnumerable<KeyValuePair<string, string>> properties)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var kv in properties)
            {
                dictionary.Add(kv.Key, kv.Value);
            }
            return dictionary;
        }

    }
}