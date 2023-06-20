using Migrations.API.Models;
using System.Data;

namespace Migrations.API.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task ExecuteBulkCopyAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default);
        Task<int> ExecuteBulkUpdateAsync(DataTable dataTable, object destinationTable, CancellationToken cancellationToken = default);
        DataTable ConvertListToDatatable(List<UserProfile> list);
    }
}
