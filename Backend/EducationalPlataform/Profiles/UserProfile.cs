using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Create → User
            CreateMap<UserCreateDto, User>();

            // Update → User
            CreateMap<UserUpdateDto, User>();

            // User → Read
            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.CourseEnrolled, opt => opt.MapFrom(src => src.CoursesEnrolled))
                .ForMember(dest => dest.CoursesCreated, opt => opt.MapFrom(src => src.CoursesCreated));
        }
    }
}

