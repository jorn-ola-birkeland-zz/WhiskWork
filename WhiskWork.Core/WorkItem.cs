using System;
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

    public class WorkItem
    {
        private readonly NameValueCollection _properties;
        private WorkItem(string id, string path, IEnumerable<string> workItemClasses, WorkItemStatus status, string parentId, int ordinal, NameValueCollection properties)
        {
            Id = id;
            Path = path;
            Classes = workItemClasses;
            Status = status;
            ParentId = parentId;
            Ordinal = ordinal;
            _properties = properties;
        }

        public static WorkItem New(string id, string path)
        {
            return New(id, path, new NameValueCollection());
        }

        public static WorkItem New(string id, string path, NameValueCollection properties)
        {
            return new WorkItem(id, path, new string[0], WorkItemStatus.Normal, null,0, properties);
        }

        public string Id { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<string> Classes { get; private set; }
        public WorkItemStatus Status  { get; private set; }
        public string ParentId { get; private set; }
        public int Ordinal { get; private set; }
        public WorkItemProperties Properties
        {
            get
            {
                return new WorkItemProperties(_properties);
            }
        }

        public WorkItem MoveTo(WorkStep step)
        {
            return new WorkItem(Id,step.Path,Classes,Status,ParentId, Ordinal,_properties);
        }

        public WorkItem UpdateStatus(WorkItemStatus status)
        {
            return new WorkItem(Id, Path, Classes, status, ParentId, Ordinal, _properties);
        }


        public WorkItem CreateChildItem(string id)
        {
            return new WorkItem(id, Path, Classes, Status, Id, Ordinal, _properties);
        }

        public WorkItem UpdateParent(WorkItem parentItem)
        {
            return new WorkItem(Id, Path, Classes, Status, parentItem.Id, Ordinal, _properties);
        }

        public WorkItem UpdateOrdinal(int ordinal)
        {
            return new WorkItem(Id, Path, Classes, Status, ParentId, ordinal, _properties);
        }

        public WorkItem AddClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes) { workItemClass };

            return new WorkItem(Id, Path, newClasses, Status, ParentId, Ordinal, _properties);
        }

        public WorkItem RemoveClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes);
            newClasses.Remove(workItemClass);

            return new WorkItem(Id, Path, newClasses, Status, ParentId, Ordinal, _properties);
        }

        public WorkItem ReplacesClasses(IEnumerable<string> newClasses)
        {
            return new WorkItem(Id, Path, newClasses, Status, ParentId, Ordinal, _properties);
        }

        public WorkItem UpdateProperties(NameValueCollection properties)
        {
            var modifiedProperties = new NameValueCollection(_properties);

            foreach (var key in properties.AllKeys)
            {
                modifiedProperties[key] = properties[key];
            }

            return new WorkItem(Id, Path, Classes, Status, ParentId, Ordinal, modifiedProperties);
        }

    }
}