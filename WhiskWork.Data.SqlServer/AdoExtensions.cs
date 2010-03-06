using System;
using System.Data;
using System.Data.SqlClient;

namespace WhiskWork.Data.Ado
{
    public static class AdoExtensions
    {
        public static string GetNullableString(this IDataRecord reader, int index)
        {
            var value = reader[index];

            if (value is DBNull)
            {
                return null;
            }
            return (string)value;
        }

        public static T? GetNullableValue<T>(this IDataRecord reader, int index) where T : struct
        {
            var o = reader[index];

            if (o is DBNull)
            {
                return null;
            }

            return (T)o;
        }

        public static void AddNullableParameter<T>(this SqlParameterCollection parameters, string name, T? value) where T : struct
        {
            if (value.HasValue)
            {
                parameters.Add(new SqlParameter(name, value.Value));
            }
            else
            {
                parameters.Add(new SqlParameter(name, DBNull.Value));
            }

        }

        public static void AddNullableString(this SqlParameterCollection parameters, string name, string value)
        {
            if (value!=null)
            {
                parameters.Add(new SqlParameter(name, value));
            }
            else
            {
                parameters.Add(new SqlParameter(name, DBNull.Value));
            }

        }

    }
}