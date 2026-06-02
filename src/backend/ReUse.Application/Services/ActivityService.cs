using AutoMapper;

using ReUse.Application.DTOs;
using ReUse.Application.DTOs.Activity;
using ReUse.Application.Exceptions;
using ReUse.Application.Interfaces;
using ReUse.Application.Interfaces.Repository;
using ReUse.Application.Interfaces.Services;
using ReUse.Domain.Entities;

namespace ReUse.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ActivityService(IActivityRepository activityRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _activityRepository = activityRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ActivityEventDto?> GetActivityByIdAsync(Guid activityId)
    {
        var entity = await _activityRepository.GetByIdAsync(activityId);
        return entity is null ? null : _mapper.Map<ActivityEventDto>(entity);
    }

    public async Task<List<ActivityEventDto>> GetUserActivitiesAsync(Guid userId, int limit = 50)
    {
        if (userId == Guid.Empty)
            throw new BadRequestException("User ID cannot be empty.");
        if (limit <= 0 || limit > 1000)
            throw new BadRequestException("Limit must be between 1 and 1000.");

        var list = await _activityRepository.GetByUserIdAsync(userId, limit);
        return _mapper.Map<List<ActivityEventDto>>(list);
    }

    public async Task CreateActivityAsync(Guid userId, Guid? productId, string type, string? description = null, string? metadata = null)
    {
        var request = new CreateActivityRequest(userId, productId, type, description, metadata);
        var entity = _mapper.Map<ActivityEvent>(request);
        _activityRepository.Add(entity);
        await _unitOfWork.SaveChangesAsync();
    }
}