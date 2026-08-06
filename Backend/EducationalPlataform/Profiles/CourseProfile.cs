using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class CourseProfile : Profile
    {
        public CourseProfile()
        {
            // DTO -> Entity

            CreateMap<CourseCreateDto, Course>();

            CreateMap<CourseUpdateDto, Course>();


            // Entity -> DTO

            CreateMap<Course, CourseReadDto>()

                .ForMember(dest => dest.TeacherName,
                    opt => opt.MapFrom(src =>
                        src.Teacher != null
                            ? src.Teacher.UserName ?? string.Empty
                            : string.Empty))

                .ForMember(dest => dest.LessonsCount,
                    opt => opt.MapFrom(src =>
                        src.Modules.Sum(m => m.Lessons.Count)))

                .ForMember(dest => dest.Modules,
                    opt => opt.MapFrom(src =>
                        src.Modules
                            .OrderBy(m => m.Order)))

                .ForMember(dest => dest.EnrolledUsers,
                    opt => opt.MapFrom(src =>
                        src.EnrolledUsers));
        }
    }
}