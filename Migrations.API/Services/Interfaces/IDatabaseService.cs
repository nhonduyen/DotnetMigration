using Migrations.API.Models;
using System.Data;

namespace Migrations.API.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task ExecuteBulkCopyAsync(DataTable dataTable, string destinationTable, CancellationToken cancellationToken = default);
        DataTable ConvertListToDatatable(List<UserProfile> list);
    }
}
