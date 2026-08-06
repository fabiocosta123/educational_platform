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

            CreateMap<Lesson, LessonReadDto>()
                .ForMember(dest => dest.TeacherName,
                    opt => opt.MapFrom(src =>
                        src.Teacher != null
                            ? src.Teacher.UserName
                            : string.Empty));
        }
    }
}