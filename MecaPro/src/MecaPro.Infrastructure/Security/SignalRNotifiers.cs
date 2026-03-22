using System.Threading.Tasks;
using MecaPro.Domain.Modules.Customers; // For Notification if needed

namespace MecaPro.Infrastructure.Security;

public interface ISignalRNotifier 
{ 
    Task NotifyUserAsync(string userId, object notification); 
}

public class MockSignalRNotifier : ISignalRNotifier
{
    public Task NotifyUserAsync(string userId, object notification) => Task.CompletedTask;
}
