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
                .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course.Title));
        }
    }
}
