#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using WhiskWork.Core;
using System.Transactions;

#endregion

namespace WhiskWork.Data.Ado
{
    public class TransactionalWorkflowRepository : WorkflowRepository
    {
        private TransactionScope _ts;

        public TransactionalWorkflowRepository(IWorkItemRepository workItemRepsitory, IWorkStepRepository workStepRepository) : base(workItemRepsitory, workStepRepository)
        {
        }

        public override IDisposable BeginTransaction()
        {
            _ts = new TransactionScope();
            return _ts;
        }

        public override void CommitTransaction()
        {
            if(_ts!=null)
            {
                _ts.Complete();
            }
        }
    }

    public class AdoWorkItemRepository : ICacheableWorkItemRepository
    {
        private const string _selectQueryPart =
            "select WI_Id, WI_Path, WI_Ordinal, WI_LastMoved, WI_Timestamp, WI_Status, WI_ParentId, WI_ParentType, WI_Properties, WI_Classes " +
            "from WI_WorkItem ";

        private readonly string _connectionString;

        public AdoWorkItemRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region ICacheableWorkItemRepository Members

        public bool ExistsWorkItem(string id)
        {
            using (var connection = new SqlConnection(_connectionString)) 
            {
                connection.Open();
                const string query = "select count(WI_Id) from WI_WorkItem where WI_Id = @Id";
                var cmd = new SqlCommand(query,connection);

                cmd.Parameters.Add(new SqlParameter("@Id", id));

                return (int) cmd.ExecuteScalar() == 1;
            }
        }

        public WorkItem GetWorkItem(string id)
        {
            const string query = _selectQueryPart + "where WI_Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var cmd = new SqlCommand(query,connection);
                cmd.Parameters.Add(new SqlParameter("@Id", id));

                return GetWorkItems(cmd).Single();
            }
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            const string query = _selectQueryPart + "where WI_Path = @Path";

            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query,connection);
                cmd.Parameters.Add(new SqlParameter("@Path", path));

                return GetWorkItems(cmd);
            }
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            const string query = _selectQueryPart + "where WI_ParentId = @ParentId and WI_ParentType = @ParentType";

            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query,connection);
                cmd.Parameters.Add(new SqlParameter("@ParentId", parent.Id));
                cmd.Parameters.Add(new SqlParameter("@ParentType", parent.Type));

                return GetWorkItems(cmd);
            }

        }

        public void CreateWorkItem(WorkItem workItem)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Create(connection,workItem);
            }
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Delete(connection, workItem.Id);
                Create(connection, workItem);
            }
        }

        public void DeleteWorkItem(string workItemId)
        {
            using(var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Delete(connection,workItemId);
            }
        }


        public IEnumerable<WorkItem> GetAllWorkItems()
        {
            const string query = _selectQueryPart;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query,connection);

                using (var reader = cmd.ExecuteReader())
                {
                    foreach (var workItem in MapWorkItem(reader))
                    {
                        yield return workItem;
                    }
                }
            }

        }

        #endregion

        private static IEnumerable<WorkItem> GetWorkItems(SqlCommand cmd)
        {
            using (var reader = cmd.ExecuteReader())
            {
                return MapWorkItem(reader);
            }
        }


        private static void AddParentParameters(SqlParameterCollection parameters, WorkItem workItem)
        {
            if (workItem.Parent != null)
            {
                parameters.Add(new SqlParameter("@ParentId", workItem.Parent.Id));
                parameters.Add(new SqlParameter("@ParentType", (int) workItem.Parent.Type));
            }
            else
            {
                parameters.Add(new SqlParameter("@ParentId", DBNull.Value));
                parameters.Add(new SqlParameter("@ParentType", DBNull.Value));
            }
        }


        private static IEnumerable<WorkItem> MapWorkItem(IDataReader reader)
        {
            var workItems = new List<WorkItem>();

            while (reader != null && reader.Read())
            {
                var workItemId = reader.GetString(0);
                var path = reader.GetString(1);

                var wi = WorkItem.New(workItemId, path);
                var ordinal = reader.GetNullableValue<int>(2);
                var lastMoved = reader.GetNullableValue<DateTime>(3);
                var timestamp = reader.GetNullableValue<DateTime>(4);
                var status = (WorkItemStatus) (int) reader[5];
                var parentId = reader.GetNullableString(6);
                var parentType = reader.GetNullableValue<WorkItemParentType>(7);
                var properties = reader.GetNullableString(8);
                var classes = reader.GetNullableString(9);

                wi = wi.UpdateStatus(status);
                wi = ordinal.HasValue ? wi.UpdateOrdinal(ordinal.Value) : wi;
                wi = lastMoved.HasValue ? wi.UpdateLastMoved(lastMoved.Value) : wi;
                wi = timestamp.HasValue ? wi.UpdateTimestamp(timestamp.Value) : wi;
                wi = properties != null ? wi.UpdateProperties(DeserializeProperties(properties)) : wi;
                wi = classes != null ? wi.UpdateClasses(classes.Split('&')) : wi;

                if (!string.IsNullOrEmpty(parentId) && parentType.HasValue)
                {
                    wi = wi.UpdateParent(parentId, parentType.Value);
                }

                workItems.Add(wi);
            }

            return workItems;
        }

        private static void Create(SqlConnection connection, WorkItem workItem)
        {
            const string workItemInsert =
                "insert into WI_WorkItem" +
                " (WI_Id,WI_Path,WI_Ordinal,WI_LastMoved,WI_Timestamp,WI_Status, WI_ParentId, WI_ParentType, WI_Properties, WI_Classes)" +
                " values (@Id,@Path,@Ordinal,@LastMoved,@Timestamp,@Status,@ParentId,@ParentType,@Properties,@Classes)";

            var cmd = new SqlCommand(workItemInsert, connection);

            cmd.Parameters.Add(new SqlParameter("@Id", workItem.Id));
            cmd.Parameters.Add(new SqlParameter("@Path", workItem.Path));

            cmd.Parameters.AddNullableParameter("@Ordinal", workItem.Ordinal);
            cmd.Parameters.AddNullableParameter("@LastMoved", workItem.LastMoved);
            cmd.Parameters.AddNullableParameter("@Timestamp", workItem.Timestamp);

            cmd.Parameters.Add(new SqlParameter("@Status", (int) workItem.Status));

            AddParentParameters(cmd.Parameters, workItem);

            cmd.Parameters.AddNullableString("@Properties", SerializeProperties(workItem));

            cmd.Parameters.AddNullableString("@Classes", SerializeClasses(workItem));

            cmd.ExecuteNonQuery();
        }


        private void Delete(SqlConnection connection, string workItemId)
        {
            const string deleteWorkItemQuery = "delete from WI_WorkItem where WI_Id = @Id";
            var cmd = new SqlCommand(deleteWorkItemQuery,connection);
            cmd.Parameters.Add(new SqlParameter("@Id", workItemId));
            cmd.ExecuteNonQuery();
        }

        private static string SerializeClasses(WorkItem workItem)
        {
            if (workItem.Classes == null || workItem.Classes.Count() == 0)
            {
                return null;
            }

            return workItem.Classes.Aggregate((current, next) => current + "&" + next);
        }

        private static string SerializeProperties(WorkItem workItem)
        {
            return workItem.Properties.Count() > 0
                       ? workItem.Properties.Select(
                             kv => HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value)).Aggregate(
                             (current, next) => current + "&" + next)
                       : null;
        }

        private static Dictionary<string, string> DeserializeProperties(string serializedProperties)
        {
            var properties = new Dictionary<string, string>();

            foreach (var keyValue in serializedProperties.Split('&'))
            {
                var kv = keyValue.Split('=');

                properties.Add(HttpUtility.UrlDecode(kv[0]), HttpUtility.UrlDecode(kv[1]));
            }

            return properties;
        }
    }
}