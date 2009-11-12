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
            var port = 5555;
            string webRootDirectory = null;
            string logFilePath = null;

            if(args.Length>0)
            {
                if(!int.TryParse(args[0], out port))
                {
                    Console.WriteLine("Not a valid port. Using default port 5555");
                    port = 5555;
                }

            }

            if(args.Length>1)
            {
                webRootDirectory = args[1];
                Console.WriteLine("Directory:{0}", webRootDirectory);
                if (!Directory.Exists(webRootDirectory))
                {
                    Console.WriteLine("Web directory does not exist. All file requests will return HTTP 403 Not found");
                    webRootDirectory = null;    
                }
            }

            if(args.Length>2)
            {
                logFilePath = args[2];
            }

            var router = new WebRouter(webRootDirectory, logFilePath);
            var server = new WebServer(router.ProcessRequest, port);

            Console.WriteLine("Started port:{0} directory:'{1}' logfile:'{2}'",port,webRootDirectory,logFilePath);
            server.Start();
        }
    }
}