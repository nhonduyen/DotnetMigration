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

        public async Task ExecuteBulkCopyAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
                using var bulk = new SqlBulkCopy(connection);
                bulk.BulkCopyTimeout = 60 * 5;
                bulk.DestinationTableName = destinationTable.GetType().Name;

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
    }
}
