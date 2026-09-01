using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseModuleProfile : Profile
    {
        public CourseModuleProfile()
        {
            CreateMap<CourseModuleCreateDto, CourseModule>();

            CreateMap<CourseModuleUpdateDto, CourseModule>();

            CreateMap<CourseModule, CourseModuleReadDto>()
                .ForMember(dest => dest.Lessons,
                    opt => opt.MapFrom(src => src.Lessons.OrderBy(l => l.Order)));
        }
    }
}