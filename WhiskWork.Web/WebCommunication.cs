using System;
using System.IO;
using System.Net;

namespace WhiskWork.Web
{
    public static class WebCommunication
    {
        public static void SendRequest(string path, string httpverb, string payload)
        {
            var request = (HttpWebRequest) WebRequest.Create("http://localhost:5555" + path);

            request.ContentType = "text/csv";
            request.Method = httpverb;
            request.ContentLength = payload.Length;

            try
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(payload);
                }

                var response = (HttpWebResponse) request.GetResponse();

                Console.WriteLine(response.StatusCode);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}