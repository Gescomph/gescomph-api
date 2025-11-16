using System;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Data.Repository;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Enum;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.AdministrationSystem
{
    public class NotificationRepository : DataGeneric<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        public override async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking()
                .Where(n => !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ToListAsync();
        }

        public override async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }

        public async Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(int userId)
        {
            return await _dbSet.AsNoTracking()
                .Where(n => n.RecipientUserId == userId && !n.IsDeleted && n.Status == NotificationStatus.Unread)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetByUserAsync(int userId, NotificationStatus? status = null, int take = 20)
        {
            var query = _dbSet.AsNoTracking()
                .Where(n => n.RecipientUserId == userId && !n.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(n => n.Status == status.Value);
            }

            query = query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id);

            if (take > 0)
            {
                query = query.Take(take);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> HasRecentNotificationAsync(int recipientUserId, NotificationType type, string? actionRoute, DateTime since)
        {
            var query = _dbSet.AsNoTracking()
                .Where(n => n.RecipientUserId == recipientUserId
                            && !n.IsDeleted
                            && n.Type == type
                            && n.CreatedAt >= since);

            query = actionRoute is null
                ? query.Where(n => n.ActionRoute == null)
                : query.Where(n => n.ActionRoute == actionRoute);

            return await query.AnyAsync();
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(n => n.RecipientUserId == userId && !n.IsDeleted && n.Status == NotificationStatus.Unread)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(n => n.Status, _ => NotificationStatus.Read)
                    .SetProperty(n => n.ReadAt, _ => now));
        }
    }
}
