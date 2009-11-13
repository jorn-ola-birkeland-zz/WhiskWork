using System;
using System.Net;
using WhiskWork.Core;
using WhiskWork.Web;

namespace WhiskWork.TestWebClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if(args.Length!=3)
            {
                Console.WriteLine("Usage: WhiskWork.TestWebClient <httpverb> <url> <csv payload>");
                return;
            }

            var httpverb = args[0];
            var url = args[1];
            var payload = args[2];

            //Console.WriteLine("url:'{0}' httpverb:'{1}' payload:'{2}'",url,httpverb,payload);

            WebCommunication.SendCsvRequest(url, httpverb, payload);

            //InitializeWorkflow();

            //RunMainLoop();
        }

        private static void InitializeWorkflow()
        {
            CreateWorkStep("/scheduled", "/", 1, WorkStepType.Begin, "cr", "Scheduled");
            CreateWorkStep("/analysis", "/", 1, WorkStepType.Normal, "cr", "Analysis");
            CreateWorkStep("/inprocess", "/analysis", 1, WorkStepType.Normal, "cr");
            CreateWorkStep("/done", "/analysis", 1, WorkStepType.Normal, "cr");
            CreateWorkStep("/development", "/", 2, WorkStepType.Begin, "cr", "Development");
            CreateWorkStep("/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            CreateWorkStep("/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            CreateWorkStep("/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            CreateWorkStep("/inprocess", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            CreateWorkStep("/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            CreateWorkStep("/done", "/development", 2, WorkStepType.End, "cr");
            CreateWorkStep("/feedback", "/", 3, WorkStepType.Parallel, "cr");
            CreateWorkStep("/review", "/feedback", 1, WorkStepType.Normal, "cr-review", "Review");
            CreateWorkStep("/test", "/feedback", 3, WorkStepType.Normal, "cr-test", "Test");
            CreateWorkStep("/done", "/", 4, WorkStepType.End, "cr", "Done");

            //CreateWorkStep("/scheduled", "/", 1, WorkStepType.Begin, "cr", "Scheduled");

            //CreateWorkStep("/wip", "/", 1, WorkStepType.Normal, "cr");
            //CreateWorkStep("/analysis", "/wip", 1, WorkStepType.Normal, "cr", "Analysis");
            //CreateWorkStep("/inprocess", "/wip/analysis", 1, WorkStepType.Normal, "cr");
            //CreateWorkStep("/done", "/wip/analysis", 1, WorkStepType.Normal, "cr");
            //CreateWorkStep("/development", "/wip", 2, WorkStepType.Begin, "cr", "Development");
            //CreateWorkStep("/inprocess", "/wip/development", 1, WorkStepType.Expand, "cr");
            //CreateWorkStep("/tasks", "/wip/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            //CreateWorkStep("/new", "/wip/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            //CreateWorkStep("/inprocess", "/wip/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            //CreateWorkStep("/done", "/wip/development/inprocess/tasks", 1, WorkStepType.End, "task");
            //CreateWorkStep("/done", "/wip/development", 2, WorkStepType.End, "cr");
            //CreateWorkStep("/feedback", "/wip", 3, WorkStepType.Parallel, "cr");
            //CreateWorkStep("/review", "/wip/feedback", 1, WorkStepType.Normal, "cr-review", "Review");
            //CreateWorkStep("/test", "/wip/feedback", 3, WorkStepType.Normal, "cr-test", "Test");

            //CreateWorkStep("/done", "/", 4, WorkStepType.End, "cr", "Done");

        }

        private static void RunMainLoop()
        {
            string line;
            do
            {
                Console.Write(">");
                line = Console.ReadLine();
                var parts = line != null ? line.Split(' ') : new string[0];
                if (parts.Length != 2 && parts.Length != 3)
                {
                    Console.WriteLine("Usage <httpverb> <path> [<workitemId>]");
                }

                var httpverb = parts[0];
                var path = parts[1];
                var payload = string.Empty;

                if (parts.Length == 3)
                {
                    payload = parts[2];
                }

                WebCommunication.SendCsvRequest("http://localhost:5555" + path, httpverb, payload);

            } while (!string.IsNullOrEmpty(line));
        }


        private static void CreateWorkStep(string step, string parentPath, int ordinal, WorkStepType type, string workItemClass)
        {
            var payload = string.Format("step={0},type={1},class={2},ordinal={3}", step, type, workItemClass, ordinal);
            WebCommunication.SendCsvRequest("http://localhost:5555" + parentPath, "post", payload);
        }

        private static void CreateWorkStep(string step, string parentPath, int ordinal, WorkStepType type, string workItemClass, string title)
        {
            var payload = string.Format("step={0},type={1},class={2},ordinal={3},title={4}", step, type, workItemClass, ordinal,title);
            WebCommunication.SendCsvRequest("http://localhost:5555" + parentPath, "post", payload);
        }
    }
}