using AutoMapper;
using FluentAssertions;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCareTests.Services;

public class RoutineServiceTests
{
    private static LifeCareDbContext NewDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<LifeCareDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;

        return new LifeCareDbContext(options);
    }

    private static IMapper NewMapper()
    {
        var cfg = new MapperConfiguration(c =>
        {
            c.CreateMap<Routine, RoutineVM>()
                .ForMember(d => d.SelectedTagIds, m => m.Ignore())
                .ForMember(d => d.AvailableTags, m => m.Ignore())
                .ForMember(d => d.ResetStats, m => m.Ignore())
                .ForMember(d => d.IsActive, m => m.Ignore());   // <── TO DODAJ

            c.CreateMap<RoutineVM, Routine>()
                .ForMember(d => d.UserId, m => m.Ignore())
                .ForMember(d => d.User, m => m.Ignore())
                .ForMember(d => d.Entries, m => m.Ignore())
                .ForMember(d => d.Tags, m => m.Ignore())
                .ForMember(d => d.Steps, m => m.Ignore()); // <<< DODAJ TO

            c.CreateMap<RoutineStep, RoutineStepVM>().ReverseMap();
            c.CreateMap<RoutineStepProduct, RoutineStepProductVM>().ReverseMap();
        });

        cfg.AssertConfigurationIsValid();
        return cfg.CreateMapper();
    }

    [Fact]
    public async Task ToggleStepAsync_CompletingLastScheduledStep_MarksEntryAndRoutineCompleted()
    {
        using var db = NewDb(nameof(ToggleStepAsync_CompletingLastScheduledStep_MarksEntryAndRoutineCompleted));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date,
            Steps = new List<RoutineStep>
            {
                new() { Name = "S1" },
                new() { Name = "S2" }
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        (await svc.ToggleStepAsync(routine.Id, routine.Steps.First().Id, today, true, null, "u1"))
            .Should().BeTrue();

        var entry1 = await db.RoutineEntries.Include(e => e.StepEntries)
            .FirstAsync();
        entry1.Completed.Should().BeFalse();

        (await svc.ToggleStepAsync(routine.Id, routine.Steps.Last().Id, today, true, null, "u1"))
            .Should().BeTrue();

        var entry2 = await db.RoutineEntries
            .Include(e => e.StepEntries)
            .FirstAsync();

        entry2.Completed.Should().BeTrue();
        entry2.StepEntries.Should().HaveCount(2);
        entry2.StepEntries.All(se => se.Completed).Should().BeTrue();
    }

    [Fact]
    public async Task ToggleStepProductAsync_AnyRotation_SetsStepCompletedWhenAnyProductCompleted()
    {
        using var db = NewDb(nameof(ToggleStepProductAsync_AnyRotation_SetsStepCompletedWhenAnyProductCompleted));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var step = new RoutineStep
        {
            Name = "ANY step",
            RotationEnabled = true,
            RotationMode = "ANY",
            Products = new List<RoutineStepProduct> { new() { Name = "P1" }, new() { Name = "P2" } }
        };
        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date,
            Steps = new List<RoutineStep> { step }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ok = await svc.ToggleStepProductAsync(routine.Id, step.Id, step.Products.First().Id, today, true, "u1");
        ok.Should().BeTrue();

        var entry = await db.RoutineEntries.Include(e => e.StepEntries).ThenInclude(se => se.ProductEntries)
            .FirstAsync();
        var se = entry.StepEntries.Single();
        se.Completed.Should().BeTrue();

        ok = await svc.ToggleStepProductAsync(routine.Id, step.Id, step.Products.First().Id, today, false, "u1");
        ok.Should().BeTrue();

        entry = await db.RoutineEntries.Include(e => e.StepEntries).ThenInclude(se => se.ProductEntries).FirstAsync();
        se = entry.StepEntries.Single();
        se.Completed.Should().BeFalse();
    }

    [Fact]
    public async Task SetAllStepsAsync_WhenAnyRotation_AlsoUpdatesProductEntries()
    {
        using var db = NewDb(nameof(SetAllStepsAsync_WhenAnyRotation_AlsoUpdatesProductEntries));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u2",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date,
            Steps = new List<RoutineStep>
            {
                new()
                {
                    Name = "ANY step",
                    RotationEnabled = true,
                    RotationMode = "ANY",
                    Products = new List<RoutineStepProduct>
                    {
                        new() { Name = "P1" },
                        new() { Name = "P2" }
                    }
                }
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ok = await svc.SetAllStepsAsync(routine.Id, today, completed: true, userId: "u2");
        ok.Should().BeTrue();

        var entry = await db.RoutineEntries
            .Include(e => e.StepEntries).ThenInclude(se => se.ProductEntries)
            .FirstAsync();

        entry.Completed.Should().BeTrue();
        var se = entry.StepEntries.Single();
        se.Completed.Should().BeTrue();
        se.ProductEntries.Should().HaveCount(2);
        se.ProductEntries.All(pe => pe.Completed).Should().BeTrue();
    }

    [Fact]
    public async Task GetForDateAsync_ReturnsOnlyStepsScheduledForThatDate()
    {
        using var db = NewDb(nameof(GetForDateAsync_ReturnsOnlyStepsScheduledForThatDate));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var start = DateTime.UtcNow.Date.AddDays(-7);
        var routine = new Routine
        {
            UserId = "u3",
            Name = "R",
            StartDateUtc = start,
            Steps = new List<RoutineStep>
            {
                new() { Name = "Daily", RRule = null },
                new() { Name = "WeeklyMon", RRule = "FREQ=WEEKLY;BYDAY=MO" },
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        DateOnly day = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        while (day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).DayOfWeek != DayOfWeek.Monday)
            day = day.AddDays(1);

        var listMonday = await svc.GetForDateAsync(day, "u3");
        listMonday.Should().HaveCount(1);
        var dayVm = listMonday.Single();
        dayVm.Steps.Select(s => s.Name).Should().Contain(new[] { "Daily", "WeeklyMon" });

        var listTue = await svc.GetForDateAsync(day.AddDays(1), "u3");
        listTue.Should().HaveCount(1);
        var tueVm = listTue.Single();
        tueVm.Steps.Select(s => s.Name).Should().Contain("Daily")
            .And.NotContain("WeeklyMon");
    }

    [Fact]
    public async Task UpdateStepOrderAsync_UpdatesOrderByProvidedList()
    {
        using var db = NewDb(nameof(UpdateStepOrderAsync_UpdatesOrderByProvidedList));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date,
            Steps = new List<RoutineStep>
            {
                new() { Name = "A", Order = 0 },
                new() { Name = "B", Order = 1 },
                new() { Name = "C", Order = 2 }
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var ids = routine.Steps.Select(s => s.Id).ToList();
        var newOrder = new List<int> { ids[2], ids[0], ids[1] };

        await svc.UpdateStepOrderAsync(routine.Id, newOrder, "u1");

        var reloaded = await db.Routines.Include(r => r.Steps).FirstAsync();
        var byName = reloaded.Steps.ToDictionary(s => s.Name, s => s.Order);
        byName["C"].Should().Be(0);
        byName["A"].Should().Be(1);
        byName["B"].Should().Be(2);
    }

    [Fact]
    public async Task GetRoutineMonthMapAsync_ReturnsExpectedStatuses()
    {
        using var db = NewDb(nameof(GetRoutineMonthMapAsync_ReturnsExpectedStatuses));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var today = DateTime.UtcNow.Date;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = firstOfMonth,
            Steps = new List<RoutineStep>
            {
                new() { Name = "Daily1", RRule = null },
                new() { Name = "Daily2", RRule = null }
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var d1 = DateOnly.FromDateTime(firstOfMonth);
        await svc.MarkRoutineCompletedAsync(routine.Id, d1, "u1");

        var d2 = d1.AddDays(1);
        var step1Id = routine.Steps.First().Id;
        await svc.ToggleStepAsync(routine.Id, step1Id, d2, completed: true, note: null, userId: "u1");

        var map = await svc.GetRoutineMonthMapAsync(routine.Id, firstOfMonth.Year, firstOfMonth.Month, "u1");

        map[$"{firstOfMonth:yyyy-MM}-01"].Should().Be("full");
        map[$"{firstOfMonth:yyyy-MM}-02"].Should().Be("partial");

        var futureDay = new DateTime(today.Year, today.Month, Math.Min(28, today.Day + 1), 0, 0, 0, DateTimeKind.Utc);
        if (futureDay.Date > today)
        {
            var key = $"{futureDay:yyyy-MM-dd}";
            map[key].Should().Be("future");
        }
    }

    [Fact]
    public async Task GetRoutineEntriesAsync_ReturnsRangeWithCounts()
    {
        using var db = NewDb(nameof(GetRoutineEntriesAsync_ReturnsRangeWithCounts));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var start = DateTime.UtcNow.Date;
        var step = new RoutineStep { Name = "Daily" };
        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = start,
            Steps = new List<RoutineStep> { step }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var d0 = DateOnly.FromDateTime(start);
        var d1 = d0.AddDays(1);
        await svc.ToggleStepAsync(routine.Id, step.Id, d0, true, null, "u1");
        await svc.ToggleStepAsync(routine.Id, step.Id, d1, false, null, "u1");

        var list = await svc.GetRoutineEntriesAsync(routine.Id, d0, d1, "u1");
        list.Should().HaveCount(2);
        list[0].TotalSteps.Should().Be(1);
        list[0].CompletedSteps.Should().Be(1);
        list[1].TotalSteps.Should().Be(1);
        list[1].CompletedSteps.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRoutineAsync_RemovesRoutine()
    {
        using var db = NewDb(nameof(DeleteRoutineAsync_RemovesRoutine));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var r = new Routine { UserId = "u1", Name = "R", StartDateUtc = DateTime.UtcNow.Date };
        db.Routines.Add(r);
        await db.SaveChangesAsync();

        await svc.DeleteRoutineAsync(r.Id, "u1");

        (await db.Routines.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateRoutineAsync_SavesRoutineWithStepsProductsAndTags()
    {
        using var db = NewDb(nameof(CreateRoutineAsync_SavesRoutineWithStepsProductsAndTags));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        db.Tags.Add(new Tag { Id = 5, UserId = "u1", Name = "Focus" });
        await db.SaveChangesAsync();

        var vm = new RoutineVM
        {
            Name = "Morning routine",
            Color = "#3b82f6",
            Icon = "fa-coffee",
            SelectedTagIds = new List<int> { 5 },
            Steps = new List<RoutineStepVM>
            {
                new()
                {
                    Name = "Shower",
                    EstimatedMinutes = 10,
                    Products = new List<RoutineStepProductVM>
                    {
                        new() { Name = "Soap" }
                    }
                }
            }
        };

        var id = await svc.CreateRoutineAsync(vm, "u1");

        var r = await db.Routines
            .Include(x => x.Tags)
            .Include(x => x.Steps).ThenInclude(s => s.Products)
            .FirstAsync();

        r.Id.Should().Be(id);
        r.Name.Should().Be("Morning routine");
        r.Tags.Should().ContainSingle(t => t.Id == 5);
        r.Steps.Should().ContainSingle(s => s.Name == "Shower");
        r.Steps.First().Products.Should().ContainSingle(p => p.Name == "Soap");
    }

    [Fact]
    public async Task UpdateRoutineAsync_WithResetStats_RemovesAllEntries()
    {
        using var db = NewDb(nameof(UpdateRoutineAsync_WithResetStats_RemovesAllEntries));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date,
            Steps = new List<RoutineStep>
            {
                new() { Name = "S1" }
            },
            Entries = new List<RoutineEntry>
            {
                new()
                {
                    Date = DateTime.UtcNow.Date,
                    StepEntries = new List<RoutineStepEntry>
                    {
                        new()
                        {
                            RoutineStepId = 1,
                            ProductEntries = new List<RoutineStepProductEntry>
                            {
                                new() { RoutineStepProductId = 1 }
                            }
                        }
                    }
                }
            }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var vm = new RoutineVM
        {
            Id = routine.Id,
            Name = "R2",
            Steps = new List<RoutineStepVM> { new() { Id = routine.Steps.First().Id, Name = "S1" } },
            ResetStats = true
        };

        await svc.UpdateRoutineAsync(vm, "u1");

        (await db.RoutineEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetRoutineStatsAsync_CalculatesStreaksAndPercentages()
    {
        using var db = NewDb(nameof(GetRoutineStatsAsync_CalculatesStreaksAndPercentages));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var start = DateTime.UtcNow.Date.AddDays(-3);
        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = start,
            Steps = new List<RoutineStep> { new() { Name = "S1" } }
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var d0 = DateOnly.FromDateTime(start);
        var d1 = d0.AddDays(1);
        var d2 = d0.AddDays(2);

        await svc.ToggleStepAsync(routine.Id, routine.Steps.First().Id, d0, true, null, "u1");
        await svc.ToggleStepAsync(routine.Id, routine.Steps.First().Id, d1, true, null, "u1");
        await svc.ToggleStepAsync(routine.Id, routine.Steps.First().Id, d2, false, null, "u1");

        var stats = await svc.GetRoutineStatsAsync(routine.Id, "u1");

        stats.CompletedDays.Should().Be(2);  
        stats.SkippedDays.Should().Be(2);     
        stats.BestStreak.Should().Be(2);

        stats.OverallPercent.Should().BeApproximately(50.0, 0.001);

    }
    
    [Fact]
    public async Task GetAllRoutinesAsync_StepUntilInPast_MarksRoutineInactive()
    {
        using var db = NewDb(nameof(GetAllRoutinesAsync_StepUntilInPast_MarksRoutineInactive));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date.AddDays(-10),
            Steps = new List<RoutineStep>
            {
                new()
                {
                    Name = "S1",
                    RRule = $"FREQ=DAILY;UNTIL={DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}"
                }
            }
        };

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var list = await svc.GetAllRoutinesAsync("u1", null);
        list.Should().HaveCount(1);
        list[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllRoutinesAsync_StepUntilInFuture_MarksRoutineActive()
    {
        using var db = NewDb(nameof(GetAllRoutinesAsync_StepUntilInFuture_MarksRoutineActive));
        var mapper = NewMapper();
        var svc = new RoutineService(db, mapper);

        var routine = new Routine
        {
            UserId = "u1",
            Name = "R",
            StartDateUtc = DateTime.UtcNow.Date.AddDays(-1),
            Steps = new List<RoutineStep>
            {
                new()
                {
                    Name = "S1",
                    RRule = $"FREQ=DAILY;UNTIL={DateTime.UtcNow.AddDays(5):yyyy-MM-dd}"
                }
            }
        };

        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        var list = await svc.GetAllRoutinesAsync("u1", null);
        list.Should().HaveCount(1);
        list[0].IsActive.Should().BeTrue();
    }

}