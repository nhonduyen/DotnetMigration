using Migrations.WorkerService.Models;
using System.Data;

namespace Migrations.WorkerService.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task ExecuteBulkCopyAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default);
        Task ExecuteMergeDataAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default);
        DataTable ConvertListToDatatable(List<UserProfile> list);
    }
}
