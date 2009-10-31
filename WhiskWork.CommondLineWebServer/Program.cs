using System;
using Abb.One.MicroWebServer;

namespace WhiskWork.CommondLineWebServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string webRootDirectory = @"c:\temp\agileboard";

            var router = new WebRouter(webRootDirectory);
            var server = new WebServer(router.ProcessRequest, 5555);

            Console.WriteLine("Started");
            server.Start();
        }
    }
}