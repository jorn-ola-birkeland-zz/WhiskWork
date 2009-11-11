using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Web
{
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