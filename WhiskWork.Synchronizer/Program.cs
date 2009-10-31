using System;
using System.IO;
using System.Net;
using System.Xml;

namespace WhiskWork.Synchronizer
{
    class Program
    {
        static void Main(string[] args)
        {
            const string path = "/";
            var request = (HttpWebRequest)WebRequest.Create("http://localhost:5555" + path);

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
                return;
            }

            foreach (XmlNode workstep in worksteps)
            {
                var id = workstep.SelectSingleNode("@id").Value;
                var workStepPath = "/" + id.Replace('.', '/');
                var workItemIds = workstep.SelectNodes("ol/li[contains(@class,'cr')]/@id");

                if(workItemIds==null)
                {
                    continue;
                }

                foreach (XmlNode workItemId in workItemIds)
                {
                    Console.WriteLine("{0}: {1}", workStepPath, workItemId.Value);
                }
            }

        }
    }
}
