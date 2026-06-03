using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            // Create → Course
            CreateMap<CourseCreateDto, Course>();

            // Update → Course
            CreateMap<CourseUpdateDto, Course>();

            // Course → Read
            CreateMap<Course, CourseReadDto>();
        }
    }
}
