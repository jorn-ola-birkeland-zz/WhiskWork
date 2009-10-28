using System;
using System.IO;
using System.Net;

namespace WhiskWork.TestWebClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string line;
            HttpWebRequest request;
            do
            {
                Console.Write(">");
                line = Console.ReadLine();
                string[] parts = line != null ? line.Split(' ') : new string[0];
                if (parts.Length != 2 && parts.Length != 3)
                {
                    Console.WriteLine("Usage <httpverb> <path> [<workitemId>]");
                }

                string httpverb = parts[0];
                string path = parts[1];
                string payload = string.Empty;

                if (parts.Length == 3)
                {
                    payload = parts[2];
                }

                request = (HttpWebRequest) WebRequest.Create("http://localhost:5555" + path);

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
            } while (!string.IsNullOrEmpty(line));
        }
    }
}