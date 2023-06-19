using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Migrations.WorkerService.Models;
using Migrations.WorkerService.Services.Interfaces;
using System.Data;

namespace Migrations.WorkerService.Services.Implements
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
            var properties = item.GetType().GetProperties().OrderBy(x => x.Name);

            foreach (var prop in properties)
            {
                dt.Columns.Add(new DataColumn(prop.Name, prop.PropertyType));
            }

            foreach (var i in list)
            {
                var row = dt.NewRow();
                var props = i.GetType().GetProperties().OrderBy(x => x.Name);
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
                using var connection = new SqlConnection(_configuration.GetConnectionString("TargetConnection"));
                connection.Open();
                using var bulk = new SqlBulkCopy(connection);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = nameof(destinationTable);

                foreach (var map in MappingColumns(destinationTable))
                {
                    bulk.ColumnMappings.Add(map);
                }
                
                await bulk.WriteToServerAsync(dataTable, cancellationToken);

                _logger.LogInformation($"Bulk copy {dataTable.Rows.Count} items sucess");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message}");
                throw ex;
            }
        }

        public async Task ExecuteMergeDataAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                var columns = GetListColumns(destinationTable); 
                string tempTableName = "#temptable_" + Guid.NewGuid().ToString("N");
                using var connection = new SqlConnection(_configuration.GetConnectionString("TargetConnection"));
                connection.Open();
                var sqlCreateTempTable = $"SELECT TOP 0 {string.Join(",", columns)} INTO {tempTableName} FROM {destinationTable.GetType().Name}";
                var command = new SqlCommand(sqlCreateTempTable, connection);
                command.CommandTimeout = 120;

                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation(sqlCreateTempTable);

                using var bulk = new SqlBulkCopy(connection);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = tempTableName;

                foreach (var map in MappingColumns(destinationTable))
                {
                    bulk.ColumnMappings.Add(map);
                }

                await bulk.WriteToServerAsync(dataTable, cancellationToken);

                var sql = BuildMergeQuery(destinationTable.GetType().Name, tempTableName, destinationTable);

                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error {ex.Message}");
                throw ex;
            }
 
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

        private List<string> GetListColumns(object o)
        {
            var columns = new List<string>();
            var properties = o.GetType().GetProperties();

            foreach (var prop in properties)
            {
                columns.Add(prop.Name);
            }
            return columns;
        }

        private string BuildMergeQuery(string src, string dest, object o)
        {
            var columns = GetListColumns(o);

            var updatePhase = columns.Where(c => !c.Equals("Id")).Select(x => $"TARGET.{x}=SOURCE.{x}");
            var insertPhase = columns.Select(x => $"SOURCE.{x}");

            var sql = string.Format(@"MERGE INTO {0} AS TARGET USING {1} AS SOURCE ON TARGET.ID=SOURCE.ID WHEN MATCHED THEN UPDATE SET {2} WHEN NOT MATCHED THEN INSERT({3}) VALUES({4});",
                src, dest, string.Join(",", updatePhase), string.Join(",", columns), string.Join(",", insertPhase));

            return sql;
        }
    }
}
