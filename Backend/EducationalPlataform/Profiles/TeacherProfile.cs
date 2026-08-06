using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class TeacherProfile : Profile
    {
        public TeacherProfile()
        {
            CreateMap<User, TeacherReadDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.UserName ?? string.Empty))

                .ForMember(dest => dest.Courses,
                    opt => opt.MapFrom(src => src.CoursesTaught));
        }
    }
}