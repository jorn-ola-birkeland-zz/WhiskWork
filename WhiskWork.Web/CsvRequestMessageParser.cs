using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using WhiskWork.Core;
using System.Linq;

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
                return ParseWorkStep(_values);
            }

            throw new ArgumentException("Unrecognized data");
        }

        protected abstract Dictionary<string,string> GetKeyValueMap(string content);

        private IWorkflowNode ParseWorkStep(Dictionary<string,string> contentParts)
        {
            //<path>,<parentPath>,<worksteptype>,<workItemClass>,<title>,<ordinal>

            var step = ExtractValue("step",s=>s,null);
            var type = ExtractValue("type", s => (WorkStepType) Enum.Parse(typeof (WorkStepType), s,true),WorkStepType.Normal);
            var workItemClass = ExtractValue("class",s=>s,null);
            var ordinal = ExtractValue("ordinal", s=> int.Parse(s), 0);
            var title = ExtractValue("title", s => s, null);

            return new WorkStepNode(step, ordinal, type, workItemClass, title);
        }

        private IWorkflowNode ParseWorkItem()
        {
            var id = ExtractValue("id", s => s, null);

            var properties = new NameValueCollection();

            foreach (KeyValuePair<string,string> keyValuePair in _values)
            {
                properties.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return new WorkItemNode(id, properties);
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

    public class CsvRequestMessageParser : RequestMessageParser
    {
        #region IRequestMessageParser Members

        protected override Dictionary<string,string> GetKeyValueMap(string content)
        {
            var values = new Dictionary<string, string>();
            foreach(var property in content.Split(','))
            {
                var pair = property.Split('=');
                if(pair.Length!=2)
                {
                    throw new ArgumentException("Illegal format");
                }
                values.Add(pair[0].ToLowerInvariant(), pair[1]);
            }

            return values;
        }

        #endregion
    }
}