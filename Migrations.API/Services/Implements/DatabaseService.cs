using Microsoft.Data.SqlClient;
using Migrations.API.Models;
using Migrations.API.Services.Interfaces;
using System.Data;
using System.Diagnostics;

namespace Migrations.API.Services.Implements
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public DataTable ConvertListToDatatable(List<UserProfile> list)
        {
            var item = list.FirstOrDefault();
            DataTable dt = new DataTable();
            var properties = item.GetType().GetProperties().Where(x => !x.Name.Equals("RowVersion"));

            foreach (var prop in properties)
            {
                dt.Columns.Add(new DataColumn(prop.Name, prop.PropertyType));
            }

            foreach (var i in list)
            {
                var row = dt.NewRow();
                var props = i.GetType().GetProperties().Where(x => !x.Name.Equals("RowVersion"));
                foreach (var prop in props)
                {
                    row[prop.Name] = prop.GetValue(i, null);
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        public async Task ExecuteBulkCopyAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();

                using var transaction = connection.BeginTransaction();
                using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = destinationTable.GetType().Name;
                bulk.BatchSize = 2000;
                bulk.NotifyAfter = 1000;

                foreach (var map in MappingColumns(destinationTable))
                {
                    bulk.ColumnMappings.Add(map);
                }

                await bulk.WriteToServerAsync(dataTable, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation($"Bulk copy {dataTable.Rows.Count} items sucess in {stopwatch.ElapsedMilliseconds} ms");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message}");
                throw ex;
            }
        }

        public async Task<int> ExecuteBulkUpdateAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                var columns = GetListColumns(destinationTable);
                string tempTableName = "#temptable_" + Guid.NewGuid().ToString("N");

                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();

                _logger.LogInformation($"Create temp table: {tempTableName}");
                var sqlCreateTempTable = $"SELECT TOP 0 {string.Join(",", columns)} INTO {tempTableName} FROM {destinationTable.GetType().Name}";
                var command = new SqlCommand(sqlCreateTempTable, connection);
                command.CommandTimeout = 120;

                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation(sqlCreateTempTable);

                using var transaction = connection.BeginTransaction();
                using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = tempTableName;
                bulk.BatchSize = 2000;
                bulk.NotifyAfter = 1000;

                foreach (var map in MappingColumns(destinationTable))
                {
                    bulk.ColumnMappings.Add(map);
                }

                await bulk.WriteToServerAsync(dataTable, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var updateQuery = BuildUpdateQuery(destinationTable.GetType().Name, tempTableName, destinationTable);

                command.CommandText = updateQuery;
                _logger.LogInformation(updateQuery);

                var result = await command.ExecuteNonQueryAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation($"Update {result} items sucess in {stopwatch.ElapsedMilliseconds} ms");

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message}");
                throw ex;
            }
        }

        private List<string> GetListColumns(object o)
        {
            var columns = new List<string>();
            var properties = o.GetType().GetProperties().Where(x => !x.Name.Equals("RowVersion"));

            foreach (var prop in properties)
            {
                columns.Add(prop.Name);
            }
            return columns;
        }

        private List<SqlBulkCopyColumnMapping> MappingColumns(object o)
        {
            var columnMapping = new List<SqlBulkCopyColumnMapping>();
            var columns = GetListColumns(o);
            foreach (var column in columns)
            {
                columnMapping.Add(new SqlBulkCopyColumnMapping(column, column));
            }
            return columnMapping;
        }

        private string BuildUpdateQuery(string src, string dest, object o)
        {
            var columns = GetListColumns(o);
            var exceptColumns = new List<string> { "Id", "CreatedAt", "RowVersion" };

            var updatePhase = columns.Where(c => !exceptColumns.Contains(c)).Select(x => $"{src}.{x}={dest}.{x}");

            var sql = string.Format(@"UPDATE {0} SET {1} FROM {0} INNER JOIN {2} ON {0}.ID = {2}.ID;", src, string.Join(",", updatePhase), dest);

            return sql;
        }
    }
}
