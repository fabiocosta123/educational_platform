using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            // DTO -> Entity

            CreateMap<UserCreateDto, User>();

            CreateMap<UserUpdateDto, User>();

            CreateMap<StudentUpdateDto, User>();


            // Entity -> DTO

            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.CourseEnrolled,
                    opt => opt.MapFrom(src => src.CourseEnrollments))

                .ForMember(dest => dest.CoursesCreated,
                    opt => opt.MapFrom(src => src.CoursesCreated));
        }
    }
}