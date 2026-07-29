using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles 
{
    public class LessonProfile : Profile
    {
        public LessonProfile()
        {
            CreateMap<LessonCreateDto, Lesson>();
            CreateMap<LessonUpdateDto, Lesson>();
            CreateMap<Lesson, LessonReadDto>();
        }
    }
}
