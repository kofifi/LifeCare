using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCareTests.Services;

public class RoutineRotationTests
{
    private static LifeCareDbContext NewDb(string name)
    {
        var opts = new DbContextOptionsBuilder<LifeCareDbContext>()
            .UseInMemoryDatabase(name)
            .EnableSensitiveDataLogging()
            .Options;
        return new LifeCareDbContext(opts);
    }

    private static IMapper NewMapper()
    {
        var cfg = new MapperConfiguration(c =>
        {
            // Entity -> VM
            c.CreateMap<Routine, RoutineVM>()
                .ForMember(d => d.SelectedTagIds, m => m.Ignore())
                .ForMember(d => d.AvailableTags, m => m.Ignore())
                .ForMember(d => d.ResetStats, m => m.Ignore());

            // VM -> Entity  (ważne: NIE mapuj Steps; serwis tworzy je ręcznie)
            c.CreateMap<RoutineVM, Routine>()
                .ForMember(d => d.UserId, m => m.Ignore())
                .ForMember(d => d.User, m => m.Ignore())
                .ForMember(d => d.Entries, m => m.Ignore())
                .ForMember(d => d.Tags, m => m.Ignore())
                .ForMember(d => d.Steps, m => m.Ignore());

            c.CreateMap<RoutineStep, RoutineStepVM>().ReverseMap();
            c.CreateMap<RoutineStepProduct, RoutineStepProductVM>().ReverseMap();
        });

        cfg.AssertConfigurationIsValid();
        return cfg.CreateMapper();
    }

    [Fact]
    public async Task Rotation_ALL_AlternatesProductsAcrossOccurrences()
    {
        using var db = NewDb(nameof(Rotation_ALL_AlternatesProductsAcrossOccurrences));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        // Start w poniedziałek (UTC): 2025-10-20
        var monday = new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        var step = new RoutineStep
        {
            Name = "Rot ALL",
            RotationEnabled = true,
            RotationMode = "ALL",
            RRule = "FREQ=WEEKLY;BYDAY=MO,WE,FR",
            Products = new List<RoutineStepProduct>
            {
                new RoutineStepProduct { Name = "P1", ProductEntries = new List<RoutineStepProductEntry>() },
                new RoutineStepProduct { Name = "P2", ProductEntries = new List<RoutineStepProductEntry>() },
            },
            StepEntries = new List<RoutineStepEntry>()
        };

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = monday,
            Steps = new List<RoutineStep> { step },
            Entries = new List<RoutineEntry>(),
            Tags = new List<Tag>()
        };

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        // 5 kolejnych wystąpień: Mo, We, Fr, Mo, We
        var dates = new[]
        {
            new DateTime(2025,10,20,0,0,0,DateTimeKind.Utc), // Mo
            new DateTime(2025,10,22,0,0,0,DateTimeKind.Utc), // We
            new DateTime(2025,10,24,0,0,0,DateTimeKind.Utc), // Fr
            new DateTime(2025,10,27,0,0,0,DateTimeKind.Utc), // next Mo
            new DateTime(2025,10,29,0,0,0,DateTimeKind.Utc), // next We
        }.Select(d => DateOnly.FromDateTime(d)).ToArray();

        string[] expected = { "P1", "P2", "P1", "P2", "P1" };

        for (int i = 0; i < dates.Length; i++)
        {
            var day = dates[i];
            var list = await svc.GetForDateAsync(day, "u1");

            list.Should().HaveCount(1);
            var vm = list.Single();

            vm.Steps.Should().HaveCount(1);
            var prod = vm.Steps.Single().Products.Should().ContainSingle().Subject;
            prod.Name.Should().Be(expected[i]);
        }
    }

    [Fact]
    public async Task Rotation_ANY_OneProductCompletionMarksStepCompleted()
    {
        using var db = NewDb(nameof(Rotation_ANY_OneProductCompletionMarksStepCompleted));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var step = new RoutineStep
        {
            Name = "Rot ANY",
            RotationEnabled = true,
            RotationMode = "ANY",
            Products = new List<RoutineStepProduct>
            {
                new RoutineStepProduct { Name = "A", ProductEntries = new List<RoutineStepProductEntry>() },
                new RoutineStepProduct { Name = "B", ProductEntries = new List<RoutineStepProductEntry>() },
            },
            StepEntries = new List<RoutineStepEntry>()
        };

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc),
            Steps = new List<RoutineStep> { step },
            Entries = new List<RoutineEntry>(),
            Tags = new List<Tag>()
        };

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var day = DateOnly.FromDateTime(new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc));

        // Odhacz tylko jeden produkt
        var ok = await svc.ToggleStepProductAsync(routine.Id, step.Id, step.Products.First().Id, day, true, "u1");
        ok.Should().BeTrue();

        var entry = await db.RoutineEntries
            .Include(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
            .FirstAsync();

        var se = entry.StepEntries.Single();
        se.Completed.Should().BeTrue("ANY → wystarczy jeden produkt");
        se.ProductEntries.Count(pe => pe.Completed).Should().Be(1);

        // Ponieważ w rutynie jest tylko jeden krok, cała rutyna (entry) powinna być ukończona
        entry.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task NoRotation_MultipleProducts_StepCompletedOnlyWhenAllProductsCompleted()
    {
        using var db = NewDb(nameof(NoRotation_MultipleProducts_StepCompletedOnlyWhenAllProductsCompleted));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var step = new RoutineStep
        {
            Name = "No rotation",
            RotationEnabled = false,   // brak rotacji
            Products = new List<RoutineStepProduct>
            {
                new RoutineStepProduct { Name = "X", ProductEntries = new List<RoutineStepProductEntry>() },
                new RoutineStepProduct { Name = "Y", ProductEntries = new List<RoutineStepProductEntry>() },
            },
            StepEntries = new List<RoutineStepEntry>()
        };

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc),
            Steps = new List<RoutineStep> { step },
            Entries = new List<RoutineEntry>(),
            Tags = new List<Tag>()
        };

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var day = DateOnly.FromDateTime(new DateTime(2025, 10, 20, 0, 0, 0, DateTimeKind.Utc));

        // 1/2 produktów → krok NIE powinien być completed
        (await svc.ToggleStepProductAsync(routine.Id, step.Id, step.Products.First().Id, day, true, "u1")).Should().BeTrue();
        var entry1 = await db.RoutineEntries
            .Include(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
            .FirstAsync();

        var se1 = entry1.StepEntries.Single();
        se1.ProductEntries.Count(pe => pe.Completed).Should().Be(1);
        se1.Completed.Should().BeFalse("brak rotacji: wymagane wszystkie produkty");

        var secondId = step.Products.Last().Id;
        (await svc.ToggleStepProductAsync(routine.Id, step.Id, secondId, day, true, "u1")).Should().BeTrue();

        var entry2 = await db.RoutineEntries
            .Include(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
            .FirstAsync();

        var se2 = entry2.StepEntries.Single();
        se2.ProductEntries.Count(pe => pe.Completed).Should().Be(2);
        se2.Completed.Should().BeTrue("po odhaczeniu wszystkich produktów krok jest wykonany");
    }
}
