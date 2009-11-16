using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        private readonly int? _ordinal;
        private WorkItem(string id, string path, IEnumerable<string> workItemClasses, WorkItemStatus status, string parentId, int? ordinal, NameValueCollection properties)
        {
            Id = id;
            Path = path;
            Classes = workItemClasses;
            Status = status;
            ParentId = parentId;
            _ordinal = ordinal;
            _properties = properties;
        }

        public static WorkItem New(string id, string path)
        {
            return New(id, path, new NameValueCollection());
        }

        public static WorkItem New(string id, string path, NameValueCollection properties)
        {
            if (!IsValidId(id))
            {
                throw new ArgumentException("Id can only consist of letters, numbers and hyphen");
            }

            return new WorkItem(id, path, new string[0], WorkItemStatus.Normal, null,null, properties);
        }

        public static WorkItem NewUnchecked(string id, string path, int? ordinal, NameValueCollection properties)
        {
            return new WorkItem(id, path, new string[0], WorkItemStatus.Normal, null, ordinal, properties);
        }


        public string Id { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<string> Classes { get; private set; }
        public WorkItemStatus Status  { get; private set; }
        public string ParentId { get; private set; }
        public int Ordinal
        {
            get
            {
                return _ordinal.HasValue ? _ordinal.Value : -1;
            }
        }

        public bool HasOrdinal
        {
            get
            {
                return _ordinal.HasValue;
            }
        }

        public WorkItemProperties Properties
        {
            get
            {
                return new WorkItemProperties(_properties);
            }
        }



        public WorkItem MoveTo(WorkStep step)
        {
            return new WorkItem(Id,step.Path,Classes,Status,ParentId, _ordinal,_properties);
        }

        public WorkItem UpdateStatus(WorkItemStatus status)
        {
            return new WorkItem(Id, Path, Classes, status, ParentId, _ordinal, _properties);
        }


        public WorkItem CreateChildItem(string id)
        {
            return new WorkItem(id, Path, Classes, Status, Id, _ordinal, _properties);
        }

        public WorkItem UpdateParent(WorkItem parentItem)
        {
            return new WorkItem(Id, Path, Classes, Status, parentItem.Id, _ordinal, _properties);
        }

        public WorkItem UpdateOrdinal(int ordinal)
        {
            return new WorkItem(Id, Path, Classes, Status, ParentId, ordinal, _properties);
        }

        public WorkItem AddClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes) { workItemClass };

            return new WorkItem(Id, Path, newClasses, Status, ParentId, _ordinal, _properties);
        }

        public WorkItem RemoveClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes);
            newClasses.Remove(workItemClass);

            return new WorkItem(Id, Path, newClasses, Status, ParentId, _ordinal, _properties);
        }

        public WorkItem ReplacesClasses(IEnumerable<string> newClasses)
        {
            return new WorkItem(Id, Path, newClasses, Status, ParentId, _ordinal, _properties);
        }

        public WorkItem UpdatePropertiesAndOrdinalFrom(WorkItem item)
        {
            var modifiedOrdinal = _ordinal;

            var modifiedProperties = GetModifiedProperties(item.Properties);

            if(item.HasOrdinal)
            {
                modifiedOrdinal = item.Ordinal;
            }

            return new WorkItem(Id, Path, Classes, Status, ParentId, modifiedOrdinal, modifiedProperties);
        }


        public WorkItem UpdateProperties(WorkItemProperties properties)
        {
            var modifiedProperties = GetModifiedProperties(properties);

            return new WorkItem(Id, Path, Classes, Status, ParentId, _ordinal, modifiedProperties);
        }

        private NameValueCollection GetModifiedProperties(WorkItemProperties propertyUpdate)
        {
            var modifiedProperties = new NameValueCollection(_properties);

            foreach (var key in propertyUpdate.AllKeys)
            {
                var newValue = propertyUpdate[key];

                if (string.IsNullOrEmpty(newValue))
                {
                    modifiedProperties.Remove(key);
                }
                else
                {
                    modifiedProperties[key] = newValue;
                }
            }
            return modifiedProperties;
        }


        public WorkItem UpdateProperties(NameValueCollection properties)
        {
            var modifiedProperties = new NameValueCollection(_properties);

            foreach (var key in properties.AllKeys)
            {
                modifiedProperties[key] = properties[key];
            }

            return new WorkItem(Id, Path, Classes, Status, ParentId, _ordinal, modifiedProperties);
        }


        public override bool Equals(object obj)
        {
            if(!(obj is WorkItem))
            {
                return false;
            }

            var item = (WorkItem) obj;

            var result = true;

            result &= Id == item.Id;
            result &= Path == item.Path;
            result &= ParentId == item.ParentId;
            result &= Status == item.Status;
            result &= Ordinal == item.Ordinal;
            result &= Classes.SequenceEqual(item.Classes);
            result &= Properties.SequenceEqual(item.Properties);

            return result;
        }

        public override int GetHashCode()
        {
            var hc = Id!=null ? Id.GetHashCode() : 1;
            hc ^= Path!=null ? Path.GetHashCode() : 2;
            hc ^= ParentId!=null ? ParentId.GetHashCode(): 4;
            hc ^= Status.GetHashCode();
            hc ^= Ordinal.GetHashCode();
            hc ^= Classes.Count()>0 ? Classes.Select(s => s.GetHashCode()).Aggregate((hash, next) => hash ^ next) : 8;
            hc ^= Properties.Count>0 ? Properties.Select(kv => kv.Key.GetHashCode() ^ kv.Value.GetHashCode()).Aggregate((hash, next) => hash ^ next) : 16;
            
            return hc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Id={0},", Id);
            sb.AppendFormat("Path={0},", Path);
            sb.AppendFormat("ParentId={0},", ParentId);
            sb.AppendFormat("Status={0},", Status);
            sb.AppendFormat("Ordinal={0},", Ordinal);
            sb.AppendFormat("Classes={0},", Classes.Count()>0 ? Classes.Aggregate((current, next) => current + "&" + next): string.Empty);
            sb.AppendFormat("Properties={0}", Properties.Count()>0 ? Properties.Select(kv => kv.Key+":"+kv.Value).Aggregate((current, next) => current + "&" + next) : string.Empty);

            return sb.ToString();
        }

        private static bool IsValidId(string workItemId)
        {
            var regex = new Regex("^[\\-,a-z,A-Z,0-9]*$");
            return regex.IsMatch(workItemId);
        }


    }
}