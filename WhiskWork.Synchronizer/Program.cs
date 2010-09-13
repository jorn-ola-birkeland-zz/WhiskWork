#region

using System;
using System.Configuration;
using System.IO;
using System.Net;

#endregion

namespace WhiskWork.Synchronizer
{
    internal class Program
    {
        private const int _numberOfMandatoryArguments = 4;

        private static void Main(string[] args)
        {
            var arguments = new CommandLineArguments(args);
            
            if(arguments.Count<_numberOfMandatoryArguments)
            {
                Console.WriteLine("Usage: WhiskWork.Synchronizer.exe <whiskwork scheme-and-authority> <eManager scheme-and-authority> <dominologin> <dominopassword> [-release:<release>] [-team:<team1[,team2,...]>]");
                return;
            }

            SynchronizeBugs(arguments);
            SynchronizeChangeRequests(arguments);
        }

        private static void SynchronizeBugs(CommandLineArguments arguments)
        {
            var whiskWorkHost = arguments[0];
            var dominoHost = arguments[1];
            var dominoLogin = arguments[2];
            var dominoPassword = arguments[3];
            var dominoLoginUrl = ConfigurationManager.AppSettings["loginUrl"];
            var bugViewUrl = ConfigurationManager.AppSettings["bugViewUrl"];

            var whiskWorkRepository = new WhiskWorkRepository(whiskWorkHost);
            var dominoRepository = new DominoRepository(dominoLogin, dominoPassword, dominoHost, dominoLoginUrl, bugViewUrl);

            var synchronizer = new ProblemReportSynchronizer(whiskWorkRepository, dominoRepository);
            synchronizer.ApplicationIdFilter = arguments.GetSafeValues("-appid");
            synchronizer.ReleaseFilter = arguments.GetSafeValues("-release");

            synchronizer.IsSafeSynch = arguments.ContainsArg("-safe");
            synchronizer.IsDryRun = arguments.ContainsArg("-dryrun");

            try
            {
                synchronizer.Synchronize();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void SynchronizeChangeRequests(CommandLineArguments arguments)
        {
            var whiskWorkHost = arguments[0];
            var dominoHost = arguments[1];
            var dominoLogin = arguments[2];
            var dominoPassword = arguments[3];
            var dominoLoginUrl = ConfigurationManager.AppSettings["loginUrl"];
            var leanViewUrl = ConfigurationManager.AppSettings["leanViewUrl"];

            var whiskWorkRepository = new WhiskWorkRepository(whiskWorkHost);
            var dominoRepository = new DominoRepository(dominoLogin, dominoPassword, dominoHost, dominoLoginUrl, leanViewUrl);

            var synchronizer = new ChangeRequestSynchronizer(whiskWorkRepository, dominoRepository);
            synchronizer.TeamFilter = arguments.GetSafeValues("-team");
            synchronizer.ReleaseFilter = arguments.GetSafeValues("-release");

            synchronizer.IsSafeSynch = arguments.ContainsArg("-safe");
            synchronizer.IsDryRun = arguments.ContainsArg("-dryrun");

            try
            {
                synchronizer.Synchronize();
            }
            catch(WebException we)
            {
                Console.WriteLine(new StreamReader(we.Response.GetResponseStream()).ReadToEnd());
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}