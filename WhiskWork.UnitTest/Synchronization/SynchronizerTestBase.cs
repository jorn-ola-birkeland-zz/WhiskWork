using System.Collections.Generic;
using Rhino.Mocks;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Core.UnitTest.Synchronization
{
    public class SynchronizerTestBase
    {
        protected MockRepository Mocks { get; set; }


        protected void Inititialize()
        {
            Mocks = new MockRepository();
        }

        protected static SynchronizationEntry Entry(string id, string status, int? ordinal)
        {
            var entry = new SynchronizationEntry(id, status, new Dictionary<string, string>()) { Ordinal = ordinal };
            return entry;
        }


        protected static SynchronizationEntry Entry(string id, string status)
        {
            return new SynchronizationEntry(id, status, new Dictionary<string, string>());
        }

        public static SynchronizationEntry Entry(string id, string status, params string[] propertyKeyValues)
        {
            var properties = new Dictionary<string, string>();

            for (int i = 0; i < propertyKeyValues.Length; i = i + 2)
            {
                properties.Add(propertyKeyValues[i], propertyKeyValues[i + 1]);
            }

            return new SynchronizationEntry(id, status, properties);
        }

        
    }
}