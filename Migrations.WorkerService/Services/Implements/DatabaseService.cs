using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

        public async Task ExecuteBulkCopyAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("TargetConnection"));
                connection.Open();
                using var bulk = new SqlBulkCopy(connection);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = destinationTable;

                foreach (var map in MappingColumns())
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

        public async Task ExecuteMergeDataAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                string tempTableName = "#temptable_" + Guid.NewGuid().ToString("N");
                using var connection = new SqlConnection(_configuration.GetConnectionString("TargetConnection"));
                connection.Open();
                var sqlCreateTempTable = $"CREATE TABLE {tempTableName}(Id uniqueidentifier primary key,Name NVARCHAR(100), Email NVARCHAR(50),Phone NVARCHAR(50),CreatedAt datetime2, LastUpdatedTime datetime2,RowVersion varbinary(18));";
                var command = new SqlCommand(sqlCreateTempTable, connection);
                command.CommandTimeout = 120;

                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation(sqlCreateTempTable);

                using var bulk = new SqlBulkCopy(connection);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = tempTableName;

                foreach (var map in MappingColumns())
                {
                    bulk.ColumnMappings.Add(map);
                }

                await bulk.WriteToServerAsync(dataTable, cancellationToken);

                var sql = string.Format(@"
MERGE INTO {0} AS TARGET USING {1} AS SOURCE ON TARGET.ID=SOURCE.ID
WHEN MATCHED THEN UPDATE SET TARGET.NAME=SOURCE.NAME, TARGET.EMAIL=SOURCE.EMAIL,TARGET.PHONE=SOURCE.PHONE, TARGET.CreatedAt=SOURCE.CreatedAt,TARGET.LastUpdatedTime=SOURCE.LastUpdatedTime,TARGET.RowVersion=SOURCE.RowVersion
WHEN NOT MATCHED THEN INSERT(ID,NAME,EMAIL,PHONE,CreatedAt,LastUpdatedTime,RowVersion) VALUES(SOURCE.ID,SOURCE.NAME,SOURCE.EMAIL,SOURCE.PHONE,SOURCE.CreatedAt,SOURCE.LastUpdatedTime,SOURCE.RowVersion);
", destinationTable, tempTableName);
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

        private List<SqlBulkCopyColumnMapping> MappingColumns()
        {
            return new List<SqlBulkCopyColumnMapping>()
            {
                   new SqlBulkCopyColumnMapping("Id", "Id"),
                   new SqlBulkCopyColumnMapping("Name", "Name"),
                   new SqlBulkCopyColumnMapping("Email", "Email"),
                   new SqlBulkCopyColumnMapping("Phone", "Phone"),
                   new SqlBulkCopyColumnMapping("CreatedAt", "CreatedAt"),
                   new SqlBulkCopyColumnMapping("LastUpdatedTime", "LastUpdatedTime"),
                   new SqlBulkCopyColumnMapping("RowVersion", "RowVersion")
            };
        }
    }
}
