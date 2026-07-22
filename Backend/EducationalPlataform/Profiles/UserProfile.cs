using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            // DTO → Entidade
            CreateMap<UserCreateDto, User>();
            CreateMap<UserUpdateDto, User>();
            CreateMap<StudentUpdateDto, User>();

            // Entidade → DTO
            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.CourseEnrolled, opt => opt.MapFrom(src => src.CourseEnrollments))
                .ForMember(dest => dest.CoursesCreated, opt => opt.MapFrom(src => src.CoursesCreated));

            CreateMap<CourseEnrollment, CourseEnrollmentReadDto>()
                .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course.Title))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Course.Teacher.UserName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.TotalLessons, opt => opt.MapFrom(src => src.Course.Lessons.Count))
                .ForMember(dest => dest.CompletedLessons, opt => opt.MapFrom(src => src.Course.Lessons.Count(l => l.IsCompleted)));

        }
    }
}
