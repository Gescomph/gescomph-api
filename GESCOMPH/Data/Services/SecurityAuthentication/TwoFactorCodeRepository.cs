using System;
using System.Linq;
using System.Threading.Tasks;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Repository;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.SecurityAuthentication
{
    public class TwoFactorCodeRepository : DataGeneric<TwoFactorCode>, ITwoFactorCodeRepository
    {
        public TwoFactorCodeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<TwoFactorCode?> GetValidCodeAsync(int userId, string code)
        {
            var normalizedCode = code?.Trim();
            if (string.IsNullOrEmpty(normalizedCode))
                return null;

            return await _dbSet.AsNoTracking()
                .Where(c => c.UserId == userId &&
                            !c.IsUsed &&
                            !c.IsDeleted &&
                            c.ExpiresAt >= DateTime.UtcNow &&
                            c.Code == normalizedCode)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TwoFactorCode?> GetLatestActiveCodeAsync(int userId)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.UserId == userId &&
                            !c.IsUsed &&
                            !c.IsDeleted &&
                            c.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidatePendingCodesAsync(int userId)
        {
            var pendingCodes = await _dbSet
                .Where(c => c.UserId == userId && !c.IsUsed && !c.IsDeleted)
                .ToListAsync();

            if (!pendingCodes.Any())
                return;

            var now = DateTime.UtcNow;
            foreach (var code in pendingCodes)
            {
                code.IsUsed = true;
                code.UsedAt = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
