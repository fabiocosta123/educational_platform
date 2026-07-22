using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            // Mapeamentos básicos
            CreateMap<CourseUpdateDto, Course>();
            CreateMap<CourseCreateDto, Course>();

            // Course → CourseReadDto
            CreateMap<Course, CourseReadDto>()
                .ForMember(dest => dest.TeacherName,
                    opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.UserName ?? string.Empty : string.Empty))
                .ForMember(dest => dest.LessonsCount,
                    opt => opt.MapFrom(src => src.Lessons != null ? src.Lessons.Count : 0))
                .ForMember(dest => dest.Lessons,
                    opt => opt.MapFrom(src => src.Lessons ?? new List<Lesson>()))
                .ForMember(dest => dest.EnrolledUsers,
                    opt => opt.MapFrom(src => src.EnrolledUsers ?? new List<CourseEnrollment>()));

            // Lesson → LessonReadDto
            CreateMap<Lesson, LessonReadDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title ?? string.Empty))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content ?? string.Empty));

            // User → UserReadDto
            CreateMap<User, UserReadDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName ?? string.Empty))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.UserEmail ?? string.Empty))
                .ForMember(dest => dest.CPF, opt => opt.MapFrom(src => src.CPF ?? string.Empty))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role ?? string.Empty));

            CreateMap<CourseEnrollment, CourseEnrollmentReadDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.UserName ?? string.Empty : string.Empty))
                .ForMember(dest => dest.CourseTitle,
                    opt => opt.MapFrom(src => src.Course != null ? src.Course.Title ?? string.Empty : string.Empty))
                .ForMember(dest => dest.TeacherName,
                    opt => opt.MapFrom(src =>
                    src.Course != null && src.Course.Teacher != null
                    ? src.Course.Teacher.UserName ?? string.Empty
                    : string.Empty))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status ?? "Ativo"));

        }
    }
}
