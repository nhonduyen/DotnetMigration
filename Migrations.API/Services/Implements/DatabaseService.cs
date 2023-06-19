using Microsoft.Data.SqlClient;
using Migrations.API.Models;
using Migrations.API.Services.Interfaces;
using System.Data;

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

        public async Task ExecuteBulkCopyAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
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

        private List<SqlBulkCopyColumnMapping> MappingColumns()
        {
            return new List<SqlBulkCopyColumnMapping>()
            {
                   new SqlBulkCopyColumnMapping("Id", "Id"),
                   new SqlBulkCopyColumnMapping("Name", "Name"),
                   new SqlBulkCopyColumnMapping("Email", "Email"),
                   new SqlBulkCopyColumnMapping("Phone", "Phone"),
                   new SqlBulkCopyColumnMapping("CreatedAt", "CreatedAt"),
                   new SqlBulkCopyColumnMapping("LastUpdatedTime", "LastUpdatedTime")
            };
        }
    }
}
