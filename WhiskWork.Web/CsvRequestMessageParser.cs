using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhiskWork.Web
{
    public class CsvFormat
    {
        public static IEnumerable<string> Parse(string content)
        {
            var quote = false;
            int start = 0;

            for(int i=0;i<content.Length;i++)
            {
                if(!quote && content[i]=='"')
                {
                    quote = true;
                    start = i + 1;
                }
                else if(!quote && start==i-1 && content[i]=='"')
                {
                    quote = true;
                    start = i+1;
                }
                else if(!quote && content[i]==',')
                {
                    yield return content.Substring(start, i - start);
                    start = i + 1;
                }
                else if(quote && content[i-1]=='"' && content[i]==',')
                {
                    yield return content.Substring(start, i - start-1);
                    quote = false;
                    start = i+1;
                }
            }

            if (quote)
            {
                if(content[content.Length - 1]=='"')
                {
                    yield return content.Substring(start, content.Length - start-1);
                }
                else
                {
                    throw new ArgumentException("Missing end quote");
                }
            }
            else
            {
                yield return content.Substring(start, content.Length - start);
            }

        }

        public static string Escape(string value)
        {
            if (value.Contains(","))
            {
                return "\"" + value + "\"";
            }

            return value;
        }

    }

    public class CsvRequestMessageParser : RequestMessageParser
    {
        #region IRequestMessageParser Members

        protected override Dictionary<string,string> GetKeyValueMap(string content)
        {
            var values = new Dictionary<string, string>();
            foreach(var property in CsvFormat.Parse(content))
            {
                var pair = property.Split('=');
                if(pair.Length!=2)
                {
                    throw new ArgumentException("Illegal format: '"+property+"'");
                }
                values.Add(HttpUtility.HtmlDecode(pair[0]).ToLowerInvariant(), HttpUtility.HtmlDecode(pair[1]));
            }

            return values;
        }

        #endregion
    }
}