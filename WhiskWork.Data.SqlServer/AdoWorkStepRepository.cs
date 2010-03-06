#region

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using WhiskWork.Core;

#endregion

namespace WhiskWork.Data.Ado
{
    public class AdoWorkStepRepository : ICacheableWorkStepRepository
    {
        private const string _selectQueryPart =
            "select WS_Path, WS_Ordinal, WS_Title, WS_Type, WS_WorkItemClass, WS_WipLimit" +
            " from WS_WorkStep ";

        private readonly string _connectionString;

        public AdoWorkStepRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region ICacheableWorkStepRepository Members

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            const string query = _selectQueryPart + "where WS_ParentPath = @Path";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add(new SqlParameter("@Path", path));

                return GetWorkSteps(cmd);
            }
        }

        public WorkStep GetWorkStep(string path)
        {
            const string query = _selectQueryPart + "where WS_Path = @Path";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add(new SqlParameter("@Path", path));

                return GetWorkSteps(cmd).Single();
            }
        }

        public IEnumerable<WorkStep> GetAllWorkSteps()
        {
            const string query = _selectQueryPart;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query, connection);

                return GetWorkSteps(cmd);
            }
        }

        public bool ExistsWorkStep(string path)
        {
            const string query = "select count(WS_Path) from WS_WorkStep where WS_Path = @Path";
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.Add(new SqlParameter("@Path", path));

                var count = (int) cmd.ExecuteScalar();

                return count == 1;
            }
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Create(connection, workStep);
            }
        }


        public void DeleteWorkStep(string path)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Delete(connection,path);
            }
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                Delete(connection,workStep.Path);
                Create(connection,workStep);
            }
        }

        #endregion

        private static IEnumerable<WorkStep> GetWorkSteps(SqlCommand cmd)
        {
            using (var reader = cmd.ExecuteReader())
            {
                return MapWorkStep(reader);
            }
        }

        private static IEnumerable<WorkStep> MapWorkStep(IDataReader reader)
        {
            var workSteps = new List<WorkStep>();

            while (reader != null && reader.Read())
            {
                var path = reader.GetString(0);
                var ordinal = reader.GetNullableValue<int>(1);
                var title = reader.GetNullableString(2);
                var type = (WorkStepType) reader.GetInt32(3);
                var workItemClass = reader.GetNullableString(4);
                var wipLimt = reader.GetNullableValue<int>(5);

                var ws = WorkStep.New(path);
                ws = ws.UpdateType(type);

                if (ordinal.HasValue)
                {
                    ws = ws.UpdateOrdinal(ordinal.Value);
                }

                if (title != null)
                {
                    ws = ws.UpdateTitle(title);
                }

                if (workItemClass != null)
                {
                    ws = ws.UpdateWorkItemClass(workItemClass);
                }

                if (wipLimt.HasValue)
                {
                    ws = ws.UpdateWipLimit(wipLimt.Value);
                }

                workSteps.Add(ws);
            }

            return workSteps;
        }

        private void Create(SqlConnection connection, WorkStep workStep)
        {
            const string workItemInsert = "insert into WS_WorkStep" +
                                          " (WS_Path,WS_ParentPath,WS_Ordinal,WS_Title, WS_Type, WS_WorkItemClass, WS_WipLimit)" +
                                          " values (@Path,@ParentPath,@Ordinal,@Title,@Type,@WorkItemClass, @WipLimit)";
            var cmd = new SqlCommand(workItemInsert,connection);

            cmd.Parameters.Add(new SqlParameter("@Path", workStep.Path));
            cmd.Parameters.Add(new SqlParameter("@ParentPath", workStep.ParentPath));
            cmd.Parameters.AddNullableParameter("@Ordinal", workStep.Ordinal);
            cmd.Parameters.AddNullableString("@Title", workStep.Title);
            cmd.Parameters.Add(new SqlParameter("@Type", (int) workStep.Type));
            cmd.Parameters.AddNullableString("@WorkItemClass", workStep.WorkItemClass);
            cmd.Parameters.AddNullableParameter("@WipLimit", workStep.WipLimit);

            cmd.ExecuteNonQuery();
        }

        private void Delete(SqlConnection connection, string path)
        {
            const string deleteWorkItemQuery = "delete from WS_WorkStep where WS_Path = @Path";
            var cmd = new SqlCommand(deleteWorkItemQuery,connection);
            cmd.Parameters.Add(new SqlParameter("@Path", path));
            cmd.ExecuteNonQuery();
        }
    }
}