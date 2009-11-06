using System;
using System.Collections.Generic;
using System.Web;

namespace WhiskWork.Web
{
    internal class FormRequestMessageParser : RequestMessageParser
    {
        protected override Dictionary<string, string> GetKeyValueMap(string content)
        {
            Console.WriteLine(content);

            var values = new Dictionary<string, string>();
            foreach (var property in content.Split('&'))
            {
                var pair = property.Split('=');
                if (pair.Length != 2)
                {
                    throw new ArgumentException("Illegal format");
                }


                values.Add(HttpUtility.UrlDecode(pair[0].ToLowerInvariant()), HttpUtility.UrlDecode(pair[1]));
            }

            return values;
        }
    }
}