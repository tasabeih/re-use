using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;

using ReUse.Application.DTOs;

using ReUse.Application.Interfaces;

using ReUse.Application.Interfaces.Repository;

using ReUse.Application.Interfaces.Services;

using ReUse.Domain.Entities;

namespace ReUse.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivityService(IActivityRepository activityRepository, IUnitOfWork unitOfWork)
    {
        _activityRepository = activityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ActivityEventDto?> GetActivityByIdAsync(Guid activityId)
    {
        var entity = await _activityRepository.GetByIdAsync(activityId);
        if (entity == null) return null;

        return new ActivityEventDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ProductId = entity.ProductId,
            Type = entity.Type,
            Description = entity.Description,
            Metadata = entity.Metadata,
            Timestamp = entity.Timestamp,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<List<ActivityEventDto>> GetUserActivitiesAsync(Guid userId, int limit = 50)
    {
        var list = await _activityRepository.GetByUserIdAsync(userId, limit);
        return list.Select(e => new ActivityEventDto
        {
            Id = e.Id,
            UserId = e.UserId,
            ProductId = e.ProductId,
            Type = e.Type,
            Description = e.Description,
            Metadata = e.Metadata,
            Timestamp = e.Timestamp,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task CreateActivityAsync(Guid userId, Guid? productId, string type, string? description = null, string? metadata = null)
    {
        var entity = new ActivityEvent
        {
            UserId = userId,
            ProductId = productId,
            Type = type,
            Description = description,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow
        };

        _activityRepository.Add(entity);
        await _unitOfWork.SaveChangesAsync();
    }
}