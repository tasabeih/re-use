using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using ReUse.Application.Interfaces.Repository;

using ReUse.Domain.Entities;

using ReUse.Infrastructure.Persistence;

namespace ReUse.Infrastructure.Repositories;

public class ActivityRepository : BaseRepository<ActivityEvent>, IActivityRepository
{
    private readonly ApplicationDbContext _context;

    public ActivityRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<ActivityEvent>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await _context.Set<ActivityEvent>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}