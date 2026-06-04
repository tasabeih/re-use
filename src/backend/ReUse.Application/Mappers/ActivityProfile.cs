using AutoMapper;

using ReUse.Application.DTOs;
using ReUse.Application.DTOs.Activity;
using ReUse.Domain.Entities;

namespace ReUse.Application.Mappers;

public class ActivityProfile : Profile
{
    public ActivityProfile()
    {

        CreateMap<ActivityEvent, ActivityEventDto>();


        CreateMap<CreateActivityRequest, ActivityEvent>()
            .ForMember(dest => dest.Timestamp,
                opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}