using AutoMapper;
using LifeCare.Models;
using LifeCare.ViewModels;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Habit, HabitVM>().ReverseMap();
        CreateMap<HabitEntry, HabitEntryVM>().ReverseMap();

        CreateMap<CategoryVM, Category>().ReverseMap();

        CreateMap<Routine, RoutineVM>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.Steps,        o => o.MapFrom(s => s.Steps));

        CreateMap<RoutineVM, Routine>()
            .ForMember(d => d.Id,       o => o.Ignore())
            .ForMember(d => d.UserId,   o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Steps,    o => o.Ignore())
            .ForMember(d => d.Entries,  o => o.Ignore());

        CreateMap<RoutineStep, RoutineStepVM>().ReverseMap();
        CreateMap<RoutineStepProduct, RoutineStepProductVM>().ReverseMap();
    }
}