using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public static class WebCommunication
    {
        public static XmlDocument GetXmlDocument(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Accept = "text/xml";
            request.Method = "GET";

            var doc = new XmlDocument();

            using(var response = (HttpWebResponse)request.GetResponse())
            {
                doc.Load(response.GetResponseStream());
            }

            return doc;
        }

        public static void SendCsvRequest(string url, string httpverb, string payload)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            request.ContentType = "text/csv";
            request.Method = httpverb;
            request.ContentLength = payload.Length;

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(payload);
            }

            var response = (IDisposable)request.GetResponse();
            response.Dispose();
        }

        public static void PostCsv(string url, IDictionary<string,string> keyValues)
        {
            var payload = CreatePayload(keyValues);

            SendCsvRequest(url,"post",payload);
        }

        public static void PostCsv(string host, WorkItem workItem)
        {
            var url = host + workItem.Path;

            var keyValues = new Dictionary<string, string> {{"id", workItem.Id}};

            if(workItem.Ordinal.HasValue)
            {
                keyValues.Add("ordinal",workItem.Ordinal.Value.ToString());
            }
            if (workItem.Timestamp.HasValue)
            {
                keyValues.Add("timestamp", XmlConvert.ToString(workItem.Timestamp.Value,XmlDateTimeSerializationMode.RoundtripKind));
            }

            foreach (var keyValuePair in workItem.Properties)
            {
                keyValues.Add(keyValuePair.Key,keyValuePair.Value);
            }

            var payload = CreatePayload(keyValues);

            Console.WriteLine("url:'{0}', payload:'{1}'",url, payload);
            SendCsvRequest(url, "post", payload);
        }

        private static string CreatePayload(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            var payloadBuilder = new StringBuilder();
            var first = true;


            foreach (var keyValuePair in keyValues)
            {
                if(!first)
                {
                    payloadBuilder.Append(",");
                }
                payloadBuilder.AppendFormat("{0}={1}", HttpUtility.HtmlEncode(keyValuePair.Key), HttpUtility.HtmlEncode(keyValuePair.Value));

                first = false;
            }

            return payloadBuilder.ToString();
        }

        public static string ReadResponseToEnd(WebResponse response)
        {
            string result;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}