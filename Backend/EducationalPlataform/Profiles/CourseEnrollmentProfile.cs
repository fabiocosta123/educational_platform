using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseEnrollmentProfile : Profile
    {
        public CourseEnrollmentProfile()
        {
            CreateMap<CourseEnrollmentCreateDto, CourseEnrollment>();

            CreateMap<CourseEnrollment, CourseEnrollmentReadDto>()

                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src =>
                        src.User != null
                            ? src.User.UserName
                            : string.Empty))

                .ForMember(dest => dest.CourseTitle,
                    opt => opt.MapFrom(src =>
                        src.Course != null
                            ? src.Course.Title
                            : string.Empty))

                .ForMember(dest => dest.TeacherName,
                    opt => opt.MapFrom(src =>
                        src.Course != null &&
                        src.Course.Teacher != null
                            ? src.Course.Teacher.UserName
                            : string.Empty))

                .ForMember(dest => dest.ProgressPercentage,
                    opt => opt.MapFrom(src => src.ProgressPercentage))

                .ForMember(dest => dest.CompletedLessons,
                    opt => opt.MapFrom(src => src.CompletedLessons))

                .ForMember(dest => dest.TotalLessons,
                    opt => opt.MapFrom(src => src.TotalLessons))

                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status));
        }
    }
}