using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public static class XmlParser
    {
        public static IEnumerable<WorkItem> ParseWorkItems(XmlNode doc, string workItemClass)
        {
            var xpath = string.Format("//WorkStep[@workItemClass='{0}']",workItemClass);
            var worksteps = doc.SelectNodes(xpath);

            return ReadWorkSteps(worksteps);
        }

        public static IEnumerable<WorkItem> ParseWorkItems(XmlNode doc)
        {
            var worksteps = doc.SelectNodes("//WorkStep");

            return ReadWorkSteps(worksteps);
        }

        private static IEnumerable<WorkItem> ReadWorkSteps(XmlNodeList worksteps)
        {
            if (worksteps == null)
            {
                yield break;
            }

            foreach (XmlNode workstep in worksteps)
            {
                var workStepId = workstep.SelectSingleNode("@id").Value;
                var workStepPath = "/" + workStepId.Replace('.', '/');
                var workItems = workstep.SelectNodes("WorkItems/WorkItem");

                if (workItems == null)
                {
                    continue;
                }

                foreach (XmlNode workItemNode in workItems)
                {
                    var workItemId = workItemNode.SelectSingleNode("@id").Value;

                    var workItem = WorkItem.New(workItemId, workStepPath);

                    workItem = ParseClasses(workItemNode, workItem);

                    workItem = workItem.UpdateProperties(CreateProperties(workItemNode));

                    workItem = ParseOrdinal(workItemNode, workItem);

                    workItem = ParseTimestamp(workItemNode, workItem);

                    workItem = ParseLastMoved(workItemNode, workItem);

                    yield return workItem;
                }
            }
        }

        private static WorkItem ParseLastMoved(XmlNode workItemNode, WorkItem workItem)
        {
            var lastMovedValue = SelectNodeValueOrDefault(workItemNode, "@lastmoved", null);
            if (!String.IsNullOrEmpty(lastMovedValue))
            {
                var lastMoved = XmlConvert.ToDateTime(lastMovedValue, XmlDateTimeSerializationMode.RoundtripKind);
                workItem = workItem.UpdateLastMoved(lastMoved);
            }
            return workItem;
        }

        private static WorkItem ParseTimestamp(XmlNode workItemNode, WorkItem workItem)
        {
            var lastUpdatedValue = SelectNodeValueOrDefault(workItemNode, "@timestamp", null);
            if(!String.IsNullOrEmpty(lastUpdatedValue))
            {
                var lastUpdated = XmlConvert.ToDateTime(lastUpdatedValue, XmlDateTimeSerializationMode.RoundtripKind);
                workItem = workItem.UpdateTimestamp(lastUpdated);
            }
            return workItem;
        }

        private static WorkItem ParseOrdinal(XmlNode workItemNode, WorkItem workItem)
        {
            var ordinal = SelectNodeValueOrDefault(workItemNode, "@ordinal", null);
            if(!String.IsNullOrEmpty(ordinal))
            {
                workItem = workItem.UpdateOrdinal(XmlConvert.ToInt32(ordinal));
            }
            return workItem;
        }

        private static WorkItem ParseClasses(XmlNode workItemNode, WorkItem workItem)
        {
            var workItemClasses = SelectNodeValueOrDefault(workItemNode,"@classes",null);
            if(!String.IsNullOrEmpty(workItemClasses))
            {
                workItem = workItem.ReplacesClasses(workItemClasses.Split(' '));
            }
            return workItem;
        }

        private static string SelectNodeValueOrDefault(XmlNode node, string xpath, string defaultValue)
        {
            var selectedNode = node.SelectSingleNode(xpath);

            return selectedNode==null ? defaultValue : selectedNode.Value;
        }

        private static NameValueCollection CreateProperties(XmlNode workItem)
        {
            var propertyNodes = workItem.SelectNodes("Properties/Property");

            var properties = new NameValueCollection();

            if (propertyNodes == null)
            {
                return properties;
            }

            foreach (XmlNode propertyNode in propertyNodes)
            {
                var key = propertyNode.SelectSingleNode("@name").Value;
                var value = propertyNode.InnerText;

                properties.Add(key, value);
            }

            return properties;
        }
    }
}