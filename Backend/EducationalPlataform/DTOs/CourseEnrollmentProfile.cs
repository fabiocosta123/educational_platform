using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseEnrollmentProfile : Profile
    {
        public CourseEnrollmentProfile()
        {
            // Create → CourseEnrollment
            CreateMap<CourseEnrollmentCreateDto, CourseEnrollment>();

            // CourseEnrollment → Read
            CreateMap<CourseEnrollment, CourseEnrollmentReadDto>()
                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                 .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course.Title))
                 .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.ProgressPercentage))
                 .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                 .ForMember(dest => dest.CompletedLessons, opt => opt.MapFrom(src => src.CompletedLessons))
                 .ForMember(dest => dest.TotalLessons, opt => opt.MapFrom(src => src.TotalLessons));
        }
    }
}
