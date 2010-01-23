using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using WhiskWork.Core;
using System.Xml;

namespace WhiskWork.Web
{
    public abstract class RequestMessageParser : IRequestMessageParser
    {
        private Dictionary<string, string> _values;

        public IWorkflowNode Parse(Stream messageStream)
        {
            var reader = new StreamReader(messageStream);
            var content = reader.ReadToEnd();

            if(string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Missing data");
            }

            _values = GetKeyValueMap(content);

            if(_values.ContainsKey("id"))
            {
                return ParseWorkItem();
            }

            if(_values.ContainsKey("step"))
            {
                return ParseWorkStep();
            }

            throw new ArgumentException("Unrecognized data");
        }

        protected abstract Dictionary<string,string> GetKeyValueMap(string content);

        private IWorkflowNode ParseWorkStep()
        {
            //<path>,<parentPath>,<worksteptype>,<workItemClass>,<title>,<ordinal>

            var node = new WorkStepNode();

            node.Step = ExtractValue("step",s=>s,null);
            node.Type = ExtractValue<WorkStepType?>("type", s => (WorkStepType) Enum.Parse(typeof (WorkStepType), s,true),null);
            node.WorkItemClass = ExtractValue("class",s=>s,null);
            node.Ordinal = ExtractValue<int?>("ordinal", s=> int.Parse(s), null);
            node.Title = ExtractValue("title", s => s, null);

            return node;
        }

        private IWorkflowNode ParseWorkItem()
        {
            var id = ExtractValue("id", s => s, null);
            var ordinal = ExtractValue<int?>("ordinal", s => int.Parse(s), null);
            var timeStamp = ExtractValue<DateTime?>("timestamp", s => XmlConvert.ToDateTime(s,XmlDateTimeSerializationMode.RoundtripKind), null);

            var properties = new NameValueCollection();

            foreach (var keyValuePair in _values)
            {
                properties.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return new WorkItemNode(id, ordinal,timeStamp, properties);
        }

        private T ExtractValue<T>(string key, Converter<string,T> convert, T defaultValue)
        {
            if(_values.ContainsKey(key))
            {
                var convertedValue = convert(_values[key]);
                _values.Remove(key);
                return convertedValue;
            }

            return defaultValue;
        }
    }
}