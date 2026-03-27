using AutoMapper;
using MiniOA.Core.Entities;
using MiniOA.Core.DTOs;
using MiniOA.Core.Enums;

namespace MiniOA.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //Entity --> DTO 数据映射至前端
            CreateMap<TodoTask, TaskDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.CreatorId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.DepartmentId))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.DepartmentFullPath, opt => opt.MapFrom(src => src.Department != null ? src.Department.FullPath : null))
                .ForMember(dest => dest.AssignedUserId, opt => opt.MapFrom(src => src.AssignedUserId))
                .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src => src.AssignedUser != null ? src.AssignedUser.FullName : null));

            // User --> UserDto 映射
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

            // DTO --> Entity 数据映射至数据库
            CreateMap<CreateTaskDto, TodoTask>()
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => myTaskStatus.Todo))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
