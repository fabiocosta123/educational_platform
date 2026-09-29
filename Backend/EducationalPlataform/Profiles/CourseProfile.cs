using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Services;

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
                .ForMember(dest => dest.InstallmentCount,
                    opt => opt.MapFrom(src =>
                        src.InstallmentCount < 1 ? 12 : src.InstallmentCount))
                .ForMember(dest => dest.InstallmentAmount,
                    opt => opt.MapFrom(src =>
                        src.Price <= 0
                            ? 0
                            : CourseInstallmentService.SplitAmount(
                                src.Price,
                                src.InstallmentCount < 1 ? 12 : src.InstallmentCount)
                              .FirstOrDefault()))

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