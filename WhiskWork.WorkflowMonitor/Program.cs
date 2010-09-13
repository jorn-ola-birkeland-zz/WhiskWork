#region

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using WhiskWork.Core;
using WhiskWork.Web;

#endregion

namespace WhiskWork.WorkflowMonitor
{
    internal class Program
    {
        private static string _connectionString;

        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: WhiskWork.WorkItemAger.exe <webhost[:port]> <connectionString>");
                return;
            }

            var host = args[0];
            _connectionString = args[1];

            var doc = new WebCommunication().GetXmlDocument(host + "/");

            var statusSnapshot = GetStatusSnapshot();

            foreach (var workItem in XmlParser.ParseWorkItems(doc))
            {
                if (statusSnapshot.ContainsKey(workItem.Id))
                {
                    var previousWorkItem = statusSnapshot[workItem.Id];

                    if (previousWorkItem.Path != workItem.Path)
                    {
                        LogMove(previousWorkItem, workItem);
                    }

                    statusSnapshot.Remove(workItem.Id);
                }
                else
                {
                    LogCreate(workItem);
                }
            }

            foreach (var workItemId in statusSnapshot.Keys)
            {
                LogDelete(statusSnapshot[workItemId]);
            }
        }

        private static Dictionary<string, WorkItem> GetStatusSnapshot()
        {
            var result = new Dictionary<string, WorkItem>();

            const string selectQuery = "select WS_Id, WS_Path from WS_WorkflowStatus";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(selectQuery, connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetString(0);
                        var path = reader.GetString(1);

                        var workItem = WorkItem.New(id, path);

                        result.Add(workItem.Id, workItem);
                    }
                }
            }


            return result;
        }

        private static void LogDelete(WorkItem workItem)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Log(connection, DateTime.Now, "delete", workItem, null);

                const string deleteStatus =
                    "delete from WS_WorkflowStatus where WS_Id=@Id and WS_Path=@Path";

                AdjustStatus(connection, workItem, deleteStatus);
            }
        }

        private static void LogCreate(WorkItem workItem)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                Log(connection, workItem.Timestamp, "create", workItem, null);

                const string createStatus =
                    "insert into WS_WorkflowStatus (WS_Id,WS_Path) VALUES (@Id,@Path)";

                AdjustStatus(connection, workItem, createStatus);
            }
        }


        private static void LogMove(WorkItem previous, WorkItem current)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Log(connection, current.LastMoved, "move", current, previous.Path);

                const string updateStatus =
                    "update WS_WorkflowStatus SET WS_Path=@Path WHERE WS_Id=@Id";

                AdjustStatus(connection, current, updateStatus);
            }
        }

        private static void Log(SqlConnection connection, DateTime? timestamp, string type, WorkItem workItem,
                                object fromPath)
        {
            const string insertLog =
                "insert into WL_WorkflowLog" +
                " (WL_Type,WL_Timestamp,WL_Id,WL_Path,WL_FromPath)" +
                " values (@Type,@Timestamp,@Id,@Path,@FromPath)";

            var cmd = new SqlCommand(insertLog, connection);

            cmd.Parameters.Add(new SqlParameter("@Type", type));
            cmd.Parameters.Add(new SqlParameter("@Timestamp", timestamp.HasValue ? timestamp.Value : DateTime.Now));
            cmd.Parameters.Add(new SqlParameter("@Id", workItem.Id));
            cmd.Parameters.Add(new SqlParameter("@Path", workItem.Path));
            cmd.Parameters.Add(new SqlParameter("@FromPath", fromPath ?? DBNull.Value));

            cmd.ExecuteNonQuery();
        }

        private static void AdjustStatus(SqlConnection connection, WorkItem workItem, string query)
        {
            var cmd = new SqlCommand(query, connection);

            cmd.Parameters.Add(new SqlParameter("@Id", workItem.Id));
            cmd.Parameters.Add(new SqlParameter("@Path", workItem.Path));

            cmd.ExecuteNonQuery();
        }
    }
}