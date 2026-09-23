using AutoMapper;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;

namespace EducationalPlataform.Profiles
{
    public class ForumProfile : Profile
    {
        public ForumProfile()
        {
            CreateMap<ForumQuestionCreateDto, ForumQuestion>();
            CreateMap<ForumQuestionUpdateDto, ForumQuestion>();

            CreateMap<ForumQuestion, ForumQuestionListDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName ?? string.Empty))
                .ForMember(d => d.LessonTitle, o => o.MapFrom(s => s.Lesson != null ? s.Lesson.Title : null))
                .ForMember(d => d.ReplyCount, o => o.MapFrom(s => s.Replies.Count));

            CreateMap<ForumQuestion, ForumQuestionReadDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName ?? string.Empty))
                .ForMember(d => d.CourseTitle, o => o.MapFrom(s => s.Course.Title ?? string.Empty))
                .ForMember(d => d.LessonTitle, o => o.MapFrom(s => s.Lesson != null ? s.Lesson.Title : null));

            CreateMap<ForumReplyCreateDto, ForumReply>();
            CreateMap<ForumReplyUpdateDto, ForumReply>();

            CreateMap<ForumReply, ForumReplyReadDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName ?? string.Empty))
                .ForMember(d => d.UserProfile, o => o.MapFrom(s => s.User.Profile.ToString()));
        }
    }
}
