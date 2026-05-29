using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

public class CourseProfile : Profile
{
    public CourseProfile()
    {
        CreateMap<Course, CourseReadDto>();
        CreateMap<CourseCreateDto, Course>();
        CreateMap<CourseUpdateDto, Course>();
    }
}
