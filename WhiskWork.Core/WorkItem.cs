using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WhiskWork.Core
{
    public enum WorkItemParentType
    {
        Expanded,
        Parallelled,
    }

    public class WorkItemParent
    {
        public WorkItemParent(string id, WorkItemParentType type) 
        {
            if(id==null)
            {
                throw new ArgumentNullException("id");
            }

            Id = id;
            Type = type;
        }

        public string Id { get; private set; }
        public WorkItemParentType Type { get; private set; }

        public override bool Equals(object obj)
        {
            if (!(obj is WorkItemParent))
            {
                return false;
            }

            var workItemParent = (WorkItemParent)obj;

            return Id == workItemParent.Id && Type == workItemParent.Type;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Type.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[{0},{1}]", Id, Type);

            return sb.ToString();
        }
    }

    public class WorkItem
    {
        private readonly NameValueCollection _properties;
        private readonly int? _ordinal;
        private WorkItem(string id, string path, IEnumerable<string> workItemClasses, WorkItemStatus status, WorkItemParent parent, int? ordinal, NameValueCollection properties, DateTime? lastUpdated, DateTime? lastMoved)
        {
            Id = id;
            Path = path;
            Classes = workItemClasses;
            Status = status;
            Parent = parent;
            Timestamp = lastUpdated;
            LastMoved = lastMoved;
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

            return new WorkItem(id, path, new string[0], WorkItemStatus.Normal, null, null, properties, null, null);
        }

        public static WorkItem NewUnchecked(string id, string path, int? ordinal, DateTime? timeStamp, NameValueCollection properties)
        {
            return new WorkItem(id, path, new string[0], WorkItemStatus.Normal, null, ordinal, properties, timeStamp, null);
        }


        public string Id { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<string> Classes { get; private set; }
        public WorkItemStatus Status  { get; private set; }
        public WorkItemParent Parent { get; private set; }
        public DateTime? Timestamp { get; private set; }
        public DateTime? LastMoved { get; private set; }

        public int? Ordinal
        {
            get
            {
                return _ordinal;
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
            return new WorkItem(Id,step.Path,Classes,Status,Parent, _ordinal,_properties, Timestamp, LastMoved);
        }

        public WorkItem UpdateStatus(WorkItemStatus status)
        {
            return new WorkItem(Id, Path, Classes, status, Parent, _ordinal, _properties, Timestamp, LastMoved);
        }


        public WorkItem CreateChildItem(string id, WorkItemParentType parentType)
        {
            var parent = new WorkItemParent(Id, parentType);
            return new WorkItem(id, Path, Classes, Status, parent, _ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem UpdateParent(WorkItem parentItem, WorkItemParentType parentType)
        {
            var parent = new WorkItemParent(parentItem.Id, parentType);
            return new WorkItem(Id, Path, Classes, Status, parent, _ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem UpdateParent(string parentId, WorkItemParentType parentType)
        {
            var parent = new WorkItemParent(parentId, parentType);
            return new WorkItem(Id, Path, Classes, Status, parent, _ordinal, _properties, Timestamp, LastMoved);
        }


        public WorkItem UpdateOrdinal(int ordinal)
        {
            return new WorkItem(Id, Path, Classes, Status, Parent, ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem AddClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes) { workItemClass };

            return new WorkItem(Id, Path, newClasses, Status, Parent, _ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem RemoveClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes);
            newClasses.Remove(workItemClass);

            return new WorkItem(Id, Path, newClasses, Status, Parent, _ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem UpdateClasses(IEnumerable<string> newClasses)
        {
            return new WorkItem(Id, Path, newClasses, Status, Parent, _ordinal, _properties, Timestamp, LastMoved);
        }

        public WorkItem UpdatePropertiesAndOrdinalFrom(WorkItem item)
        {
            var modifiedOrdinal = _ordinal;

            var modifiedProperties = GetModifiedProperties(item.Properties);

            if(item.Ordinal.HasValue)
            {
                modifiedOrdinal = item.Ordinal;
            }

            return new WorkItem(Id, Path, Classes, Status, Parent, modifiedOrdinal, modifiedProperties, Timestamp, LastMoved);
        }


        public WorkItem UpdateProperties(WorkItemProperties properties)
        {
            var modifiedProperties = GetModifiedProperties(properties);

            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, modifiedProperties, Timestamp, LastMoved);
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

        public WorkItem UpdateProperty(string name, string value)
        {
            var modifiedProperties = new NameValueCollection(_properties);
            modifiedProperties[name] = value;

            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, modifiedProperties, Timestamp, LastMoved);
        }


        public WorkItem UpdateProperties(NameValueCollection properties)
        {
            var modifiedProperties = new NameValueCollection(_properties);

            foreach (var key in properties.AllKeys)
            {
                modifiedProperties[key] = properties[key];
            }

            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, modifiedProperties, Timestamp, LastMoved);
        }

        public WorkItem UpdateProperties(Dictionary<string, string> properties)
        {
            var modifiedProperties = new NameValueCollection(_properties);

            foreach (var key in properties.Keys)
            {
                modifiedProperties[key] = properties[key];
            }

            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, modifiedProperties, Timestamp, LastMoved);
        }


        public WorkItem UpdateTimestamp(DateTime timeStamp)
        {
            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, _properties, timeStamp, LastMoved);
        }

        public WorkItem UpdateLastMoved(DateTime lastMoved)
        {
            return new WorkItem(Id, Path, Classes, Status, Parent, _ordinal, _properties, Timestamp, lastMoved);
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
            result &= Parent == item.Parent;
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
            hc ^= Parent!=null ? Parent.GetHashCode(): 4;
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
            sb.AppendFormat("Parent={0},", Parent);
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


        public WorkItemParent AsParent(WorkItemParentType parentType)
        {
            return new WorkItemParent(Id,parentType);
        }

    }
}