using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.SqlServer.Server;

namespace Dapperer.QueryBuilders.MsSql
{
    public abstract class TableValuedParams : SqlMapper.IDynamicParameters
    {
        private readonly string _tableValuedParam;
        private readonly List<SqlParameter> _otherParams;
        private readonly string _tableValuedTypeName;

        protected TableValuedParams(string tableValuedParam, string tableValuedTypeName)
        {
            _otherParams = new List<SqlParameter>();
            _tableValuedParam = tableValuedParam;
            _tableValuedTypeName = tableValuedTypeName;
        }

        protected abstract List<SqlDataRecord> GenerateTableParameterRecords();

        public void AdditionalParameter(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                Value = value,
                Direction = direction ?? ParameterDirection.Input
            };
            if (size.HasValue)
            {
                parameter.Size = size.Value;
            }
            if (dbType != null)
            {
                parameter.DbType = dbType.Value;
            }
            _otherParams.Add(parameter);
        }

        public void AdditionalIntTableValuedParameter(string name, IEnumerable<int> items)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = "IntList",
                Value = GenerateIntTableParameterRecords(items)
            };

            _otherParams.Add(parameter);
        }

        public void AdditionalLongTableValuedParameter(string name, IEnumerable<long> items)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = "LongList",
                Value = GenerateLongTableParameterRecords(items)
            };

            _otherParams.Add(parameter);
        }

        public void AdditionalStringTableValuedParameter(string name, IEnumerable<string> items)
        {
            var parameter = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = "StringList",
                Value = GenerateStringTableParameterRecords(items)
            };

            _otherParams.Add(parameter);
        }

        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            var sqlCommand = (SqlCommand)command;
            sqlCommand.CommandType = CommandType.Text;

            // Add the table parameter.
            var tableParam = sqlCommand.Parameters.Add(_tableValuedParam, SqlDbType.Structured);
            tableParam.TypeName = _tableValuedTypeName;
            tableParam.Value = GenerateTableParameterRecords();

            // Add other additional parameters
            foreach (var param in _otherParams)
            {
                sqlCommand.Parameters.Add(param);
            }
        }

        protected List<SqlDataRecord> GenerateIntTableParameterRecords(IEnumerable<int> items)
        {
            if (IsNullOrEmpty(items))
            {
                return null;
            }

            SqlMetaData[] tableDefinition = { new SqlMetaData("Id", SqlDbType.Int) };

            return items.Select(item =>
            {
                var record = new SqlDataRecord(tableDefinition);
                record.SetInt32(0, item);
                return record;
            }).ToList();
        }

        protected List<SqlDataRecord> GenerateLongTableParameterRecords(IEnumerable<long> items)
        {
            if (IsNullOrEmpty(items))
            {
                return null;
            }

            SqlMetaData[] tableDefinition = { new SqlMetaData("Id", SqlDbType.BigInt) };

            return items.Select(item =>
            {
                var record = new SqlDataRecord(tableDefinition);
                record.SetInt64(0, item);
                return record;
            }).ToList();
        }

        protected List<SqlDataRecord> GenerateStringTableParameterRecords(IEnumerable<string> items)
        {
            if (IsNullOrEmpty(items))
            {
                return null;
            }

            SqlMetaData[] tableDefinition = { new SqlMetaData("Id", SqlDbType.NVarChar, 100) };

            return items.Select(item =>
            {
                var record = new SqlDataRecord(tableDefinition);
                record.SetString(0, item);
                return record;
            }).ToList();
        }

        public bool IsNullOrEmpty<TSource>(IEnumerable<TSource> source)
        {
            return source == null || !source.Any();
        }
    }
}
