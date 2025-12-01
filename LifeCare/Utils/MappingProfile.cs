using AutoMapper;
using LifeCare.Models;
using LifeCare.ViewModels;

namespace LifeCare.Utils;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Habit, HabitVM>()
            .ForMember(d => d.SelectedTagIds, o => o.MapFrom(s => s.Tags.Select(t => t.Id)))
            .ForMember(d => d.AvailableTags, o => o.Ignore());

        CreateMap<HabitVM, Habit>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore())
            .ForMember(d => d.Entries, o => o.Ignore());

        CreateMap<HabitEntry, HabitEntryVM>().ReverseMap();

        CreateMap<RoutineStep, RoutineStepVM>().ReverseMap();
        CreateMap<RoutineStepProduct, RoutineStepProductVM>().ReverseMap();

        CreateMap<Routine, RoutineVM>()
            .ForMember(d => d.Steps, o => o.MapFrom(s => s.Steps))
            .ForMember(d => d.SelectedTagIds, o => o.MapFrom(s => s.Tags.Select(t => t.Id)))
            .ForMember(d => d.AvailableTags, o => o.Ignore())
            .ForMember(d => d.IsActive, m => m.Ignore());

        CreateMap<RoutineVM, Routine>()
            .ForMember(d => d.Id,       o => o.Ignore())
            .ForMember(d => d.UserId,   o => o.Ignore())
            .ForMember(d => d.Steps,    o => o.Ignore())
            .ForMember(d => d.Entries,  o => o.Ignore())
            .ForMember(d => d.Tags,     o => o.Ignore());

        CreateMap<Tag, TagVM>().ReverseMap();
    }
}