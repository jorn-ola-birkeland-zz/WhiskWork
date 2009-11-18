using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WhiskWork.Core
{
    public class WorkItemProperties : IEnumerable<KeyValuePair<string,string>>
    {
        private readonly NameValueCollection _properties;

        public WorkItemProperties(NameValueCollection properties)
        {
            _properties = properties;
        }

        public int Count
        {
            get { return _properties.Count;  }
        }

        public IEnumerable<string> AllKeys
        {
            get { return _properties.AllKeys; }
        }

        public string this[string key]
        {
            get
            {
                return _properties[key];
            }
        }

        public IEnumerator<KeyValuePair<string,string>> GetEnumerator()
        {
            foreach (var key in _properties.AllKeys)
            {
                yield return new KeyValuePair<string, string>(key,_properties[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}