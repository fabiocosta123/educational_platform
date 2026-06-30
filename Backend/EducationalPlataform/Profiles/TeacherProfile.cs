using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

public class TeacherProfile : Profile
{
    public TeacherProfile()
    {
        // Ajuste conforme o nome da propriedade na entidade User
        CreateMap<User, TeacherReadDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
    }
}
