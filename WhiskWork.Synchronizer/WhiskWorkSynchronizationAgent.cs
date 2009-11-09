using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Synchronizer
{
    class WhiskWorkSynchronizationAgent : ISynchronizationAgent
    {
        private readonly string _site;
        private readonly string _rootPath;
        private readonly string _beginStep;

        public WhiskWorkSynchronizationAgent(string site, string rootPath, string beginStepPath)
        {
            _site = site;
            _rootPath = rootPath;
            _beginStep = beginStepPath;
        }

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + _rootPath);

            request.ContentType = "text/html";
            request.Method = "GET";

            var doc = new XmlDocument();
            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                Console.WriteLine(response.StatusCode);

                doc.Load(response.GetResponseStream());

            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }

            var worksteps = doc.SelectNodes("//li[contains(@class,'step-cr')]");

            if (worksteps == null)
            {
                yield break;
            }

            foreach (XmlNode workstep in worksteps)
            {
                var workStepId = workstep.SelectSingleNode("@id").Value;
                var workStepPath = "/" + workStepId.Replace('.', '/');
                var workItems = workstep.SelectNodes("ol/li[contains(@class,'cr')]");

                if (workItems == null)
                {
                    continue;
                }

                foreach (XmlNode workItem in workItems)
                {
                    var properties = CreateProperties(workItem);
                    var workItemId = workItem.SelectSingleNode("@id").Value;
                    yield return new SynchronizationEntry(workItemId,workStepPath,properties);
                }
            }
        }

        private static Dictionary<string,string> CreateProperties(XmlNode workItem)
        {
            var definitionNodes = workItem.SelectNodes("dl/dt");

            var properties = new Dictionary<string, string>();

            if(definitionNodes==null)
            {
                return properties;
            }

            foreach (XmlNode definitionNode in definitionNodes)
            {
                var key = definitionNode.SelectSingleNode("@class").Value;
                var value = workItem.SelectSingleNode(string.Format("dl/dd[@class='{0}']",key)).InnerText;

                properties.Add(key,value);
            }

            return properties;
        }

        public void Create(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Create:"+entry);
            var payload = CreatePayload(entry);

            PostCsv(payload, _beginStep);

            PostCsv(payload, entry.Status);
        }

        public void Delete(SynchronizationEntry entry)
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + entry.Status + "/" + entry.Id);
            request.ContentType = "text/csv";
            request.Method = "delete";

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(response.StatusCode);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            var payload = string.Format("id={0}", entry.Id);

            PostCsv(payload, entry.Status);
        }

        public void UpdateProperties(SynchronizationEntry entry)
        {
            var payload = CreatePayload(entry);

            PostCsv(payload, entry.Status);
        }

        private static string CreatePayload(SynchronizationEntry entry)
        {
            var payloadBuilder = new StringBuilder();
            payloadBuilder.AppendFormat("id={0}", entry.Id);

            foreach (var keyValuePair in entry.Properties)
            {
                payloadBuilder.AppendFormat(",{0}={1}", HttpUtility.HtmlEncode(keyValuePair.Key), HttpUtility.HtmlEncode(keyValuePair.Value));
            }
            return payloadBuilder.ToString();
        }


        private void PostCsv(string payload, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + path);
            request.ContentType = "text/csv";
            request.Method = "post";
            request.ContentLength = payload.Length;

            try
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(payload);
                }

                var response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}