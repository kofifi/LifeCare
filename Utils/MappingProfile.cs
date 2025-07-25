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
    }
}