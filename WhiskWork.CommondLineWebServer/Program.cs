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
            string storageType = null;
            string connectionString = null;

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

            if(args.Length>3)
            {
                storageType = args[3];
            }

            if(args.Length>4)
            {
                connectionString = args[4];
            }

            var workflowRepository = ParseRepository(storageType, connectionString).WorkflowRepository;

            var router = new WebRouter(workflowRepository, webRootDirectory, logFilePath);
            var server = new WebServer(router.ProcessRequest, port);

            Console.WriteLine("Started port:{0} directory:'{1}' logfile:'{2}'",port,webRootDirectory,logFilePath);
            server.Start();
        }

        private static IWorkflowRepositoryFactory ParseRepository(string storageType, string connectionString)
        {
            switch(storageType.ToLowerInvariant())
            {
                case "-aws":
                    var parameters = connectionString.Split(';');
                    var awsAccessKey = parameters[0];
                    var awsSecretAccessKey = parameters[1];
                    var domainPrefix = parameters[2];

                    return new SimpleDbWorkflowRepositoryFactory(domainPrefix,awsAccessKey,awsSecretAccessKey);
                case "-sql":
                case "-ado":
                    return new AdoWorkflowRepositoryFactory(connectionString);

                default:
                    return new MemoryWorkflowRepositoryFactory();

            }
        }
    }
}