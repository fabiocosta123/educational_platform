using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            // Update → Course
            CreateMap<CourseUpdateDto, Course>();

            CreateMap<Course, CourseReadDto>()
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.UserName));

            CreateMap<CourseCreateDto, Course>();

        }
    }
}
