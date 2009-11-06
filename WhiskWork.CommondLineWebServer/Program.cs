using System;
using Abb.One.MicroWebServer;
using System.IO;

namespace WhiskWork.CommondLineWebServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            const string webRootDirectory = @"..\..\..\Example";

            var router = new WebRouter(webRootDirectory);
            var server = new WebServer(router.ProcessRequest, 5555);

            Console.WriteLine("Started");
            server.Start();
        }
    }
}