using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

public class TeacherProfile : Profile
{
    public TeacherProfile()
    {
        CreateMap<User, TeacherReadDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName ?? string.Empty))
            .ForMember(dest => dest.Courses, opt => opt.MapFrom(src => src.CoursesTaught ?? new List<Course>()));

        CreateMap<Course, CourseReadDto>()
            .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.UserName ?? string.Empty : string.Empty))
            .ForMember(dest => dest.LessonsCount, opt => opt.MapFrom(src => src.Lessons != null ? src.Lessons.Count : 0))
            .ForMember(dest => dest.Lessons, opt => opt.MapFrom(src => src.Lessons ?? new List<Lesson>()))
            .ForMember(dest => dest.EnrolledUsers, opt => opt.MapFrom(src => src.EnrolledUsers ?? new List<CourseEnrollment>()));
    }
}
