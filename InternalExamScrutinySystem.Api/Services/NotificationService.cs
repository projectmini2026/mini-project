using InternalExamScrutinySystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace InternalExamScrutinySystem.Api.Services;

public interface INotificationService
{
    Task<List<Notification>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken);
    Task MarkAsReadAsync(int userId, CancellationToken cancellationToken);
    Task SendNotificationToUsersAsync(List<int> userIds, string message, CancellationToken cancellationToken);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Notification>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken)
    {
        return _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int userId, CancellationToken cancellationToken)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SendNotificationToUsersAsync(List<int> userIds, string message, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}

