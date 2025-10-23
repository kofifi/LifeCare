using AutoMapper;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LifeCare.Services
{
    public class RoutineService : IRoutineService
    {
        private readonly LifeCareDbContext _db;
        private readonly IMapper _mapper;

        public RoutineService(LifeCareDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<RoutineVM>> GetAllRoutinesAsync(string userId, List<int>? tagIds)
        {
            var query = _db.Routines
                .AsNoTracking()
                .Include(r => r.Tags)
                .Include(r => r.Steps)
                .Where(r => r.UserId == userId);

            if (tagIds != null && tagIds.Count > 0)
            {
                query = query.Where(r => r.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            var list = await query
                .OrderBy(r => r.Order)
                .ToListAsync();

            var result = list.Select(r =>
            {
                var vm = _mapper.Map<RoutineVM>(r);
                vm.SelectedTagIds = r.Tags?.Select(t => t.Id).ToList() ?? new List<int>();
                return vm;
            }).ToList();

            return result;
        }

        public async Task<RoutineVM?> GetRoutineAsync(int id, string userId)
        {
            var entity = await _db.Routines
                .AsNoTracking()
                .Include(r => r.Tags)
                .Include(r => r.Steps).ThenInclude(s => s.Products)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Id == id);

            if (entity == null) return null;

            var vm = _mapper.Map<RoutineVM>(entity);
            vm.SelectedTagIds = entity.Tags?.Select(t => t.Id).ToList() ?? new List<int>();
            vm.AvailableTags = await _db.Tags
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .Select(t => new TagVM { Id = t.Id, Name = t.Name })
                .ToListAsync();

            return vm;
        }

        public async Task<int> CreateRoutineAsync(RoutineVM vm, string userId)
        {
            var entity = _mapper.Map<Routine>(vm);
            entity.UserId = userId;

            if (entity.StartDateUtc == default)
                entity.StartDateUtc = DateTime.UtcNow.Date;

            var maxOrder = await _db.Routines
                .Where(r => r.UserId == userId)
                .Select(r => (int?)r.Order)
                .MaxAsync();

            entity.Order = (maxOrder ?? -1) + 1;

            if (vm.SelectedTagIds != null && vm.SelectedTagIds.Count > 0)
            {
                var tags = await _db.Tags
                    .Where(t => t.UserId == userId && vm.SelectedTagIds.Contains(t.Id))
                    .ToListAsync();
                foreach (var t in tags) entity.Tags.Add(t);
            }

            _db.Routines.Add(entity);
            await _db.SaveChangesAsync();

            if (vm.Steps != null && vm.Steps.Count > 0)
            {
                int order = 0;
                foreach (var s in vm.Steps)
                {
                    if (string.IsNullOrWhiteSpace(s.Name)) continue;

                    var step = new RoutineStep
                    {
                        RoutineId = entity.Id,
                        Name = s.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(s.Description) ? null : s.Description.Trim(),
                        Action = string.IsNullOrWhiteSpace(s.Action) ? null : s.Action.Trim(),
                        EstimatedMinutes = s.EstimatedMinutes,
                        Order = s.Order != 0 ? s.Order : order++,
                        RRule = string.IsNullOrWhiteSpace(s.RRule) ? null : s.RRule.Trim(),
                        RotationEnabled = s.RotationEnabled,
                        RotationMode = string.IsNullOrWhiteSpace(s.RotationMode) ? null : s.RotationMode!.Trim()
                    };

                    if (s.Products != null && s.Products.Count > 0)
                    {
                        step.Products = s.Products
                            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                            .Select(p => new RoutineStepProduct
                            {
                                Name = p.Name!.Trim(),
                                Note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note!.Trim(),
                                Url = string.IsNullOrWhiteSpace(p.Url) ? null : p.Url!.Trim(),
                                ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl!.Trim()
                            })
                            .ToList();
                    }

                    _db.RoutineSteps.Add(step);
                }

                await _db.SaveChangesAsync();
            }

            return entity.Id;
        }

        public async Task UpdateRoutineAsync(RoutineVM vm, string userId)
        {
            var entity = await _db.Routines
                .Include(r => r.Tags)
                .Include(r => r.Steps).ThenInclude(s => s.Products)
                .Include(r => r.Entries)
                .ThenInclude(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
                .FirstOrDefaultAsync(r => r.Id == vm.Id && r.UserId == userId);

            if (entity == null) throw new KeyNotFoundException();

            var oldStart = entity.StartDateUtc.Date;
            var newStart = (vm.StartDateUtc == default ? oldStart : vm.StartDateUtc.Date);

            _mapper.Map(vm, entity);
            entity.StartDateUtc = newStart;

            entity.Tags.Clear();
            if (vm.SelectedTagIds != null && vm.SelectedTagIds.Count > 0)
            {
                var tags = await _db.Tags
                    .Where(t => t.UserId == userId && vm.SelectedTagIds.Contains(t.Id))
                    .ToListAsync();
                foreach (var t in tags) entity.Tags.Add(t);
            }

            if (vm.ResetStats)
            {
                foreach (var e in entity.Entries.ToList())
                {
                    foreach (var se in e.StepEntries.ToList())
                    {
                        if (se.ProductEntries != null && se.ProductEntries.Count > 0)
                            _db.RemoveRange(se.ProductEntries);

                        _db.Remove(se);
                    }

                    _db.Remove(e);
                }

                entity.Entries.Clear();
            }

            if (vm.Steps != null)
            {
                var existingById = entity.Steps.ToDictionary(s => s.Id);
                var incomingIds = vm.Steps.Where(s => s.Id != 0).Select(s => s.Id).ToHashSet();

                foreach (var toDel in entity.Steps.Where(s => !incomingIds.Contains(s.Id)).ToList())
                {
                    foreach (var e in entity.Entries)
                    {
                        var seForStep = e.StepEntries.Where(se => se.RoutineStepId == toDel.Id).ToList();
                        foreach (var se in seForStep)
                        {
                            if (se.ProductEntries != null && se.ProductEntries.Count > 0)
                                _db.RemoveRange(se.ProductEntries);
                            _db.Remove(se);
                        }
                    }

                    _db.RoutineSteps.Remove(toDel);
                }

                int nextOrder = 0;

                foreach (var s in vm.Steps)
                {
                    if (s.Id != 0 && existingById.TryGetValue(s.Id, out var step))
                    {
                        step.Name = s.Name?.Trim();
                        step.Description = string.IsNullOrWhiteSpace(s.Description) ? null : s.Description.Trim();
                        step.Action = string.IsNullOrWhiteSpace(s.Action) ? null : s.Action.Trim();
                        step.EstimatedMinutes = s.EstimatedMinutes;
                        step.RRule = string.IsNullOrWhiteSpace(s.RRule) ? null : s.RRule.Trim();
                        step.Order = s.Order != 0 ? s.Order : nextOrder++;
                        step.RotationEnabled = s.RotationEnabled;
                        step.RotationMode = string.IsNullOrWhiteSpace(s.RotationMode) ? null : s.RotationMode;

                        var incoming = (s.Products ?? new List<RoutineStepProductVM>())
                            .Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();

                        step.Products ??= new List<RoutineStepProduct>();
                        var keepIds = incoming.Where(p => p.Id != 0).Select(p => p.Id).ToHashSet();

                        foreach (var pd in step.Products.Where(p => !keepIds.Contains(p.Id)).ToList())
                            _db.RoutineStepProducts.Remove(pd);

                        var byId = step.Products.ToDictionary(p => p.Id);
                        foreach (var p in incoming)
                        {
                            if (p.Id != 0 && byId.TryGetValue(p.Id, out var ep))
                            {
                                ep.Name = p.Name!.Trim();
                                ep.Note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note!.Trim();
                                ep.Url = string.IsNullOrWhiteSpace(p.Url) ? null : p.Url!.Trim();
                                ep.ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl!.Trim();
                            }
                            else
                            {
                                step.Products.Add(new RoutineStepProduct
                                {
                                    Name = p.Name!.Trim(),
                                    Note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note!.Trim(),
                                    Url = string.IsNullOrWhiteSpace(p.Url) ? null : p.Url!.Trim(),
                                    ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl!.Trim()
                                });
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(s.Name))
                    {
                        var newStep = new RoutineStep
                        {
                            RoutineId = entity.Id,
                            Name = s.Name.Trim(),
                            Description = string.IsNullOrWhiteSpace(s.Description) ? null : s.Description.Trim(),
                            Action = string.IsNullOrWhiteSpace(s.Action) ? null : s.Action.Trim(),
                            EstimatedMinutes = s.EstimatedMinutes,
                            Order = s.Order != 0 ? s.Order : nextOrder++,
                            RRule = string.IsNullOrWhiteSpace(s.RRule) ? null : s.RRule.Trim(),
                            RotationEnabled = s.RotationEnabled,
                            RotationMode = string.IsNullOrWhiteSpace(s.RotationMode) ? null : s.RotationMode
                        };

                        if (s.Products != null && s.Products.Count > 0)
                        {
                            newStep.Products = s.Products
                                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                                .Select(p => new RoutineStepProduct
                                {
                                    Name = p.Name!.Trim(),
                                    Note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note!.Trim(),
                                    Url = string.IsNullOrWhiteSpace(p.Url) ? null : p.Url!.Trim(),
                                    ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl!.Trim()
                                })
                                .ToList();
                        }

                        _db.RoutineSteps.Add(newStep);
                    }
                }
            }

            await _db.SaveChangesAsync();

            if (!vm.ResetStats && newStart > oldStart)
            {
                var toRemove = entity.Entries.Where(e => e.Date.Date < newStart).ToList();
                foreach (var e in toRemove)
                {
                    foreach (var se in e.StepEntries.ToList())
                    {
                        if (se.ProductEntries != null && se.ProductEntries.Count > 0)
                            _db.RemoveRange(se.ProductEntries);
                        _db.Remove(se);
                    }

                    _db.Remove(e);
                }

                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteRoutineAsync(int id, string userId)
        {
            var entity = await _db.Routines.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (entity == null) return;
            _db.Routines.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStepOrderAsync(int routineId, List<int> orderedStepIds, string userId)
        {
            var routine = await _db.Routines
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);
            if (routine == null) return;

            var orderMap = orderedStepIds
                .Select((id, idx) => new { id, idx })
                .ToDictionary(x => x.id, x => x.idx);

            foreach (var s in routine.Steps)
                if (orderMap.TryGetValue(s.Id, out var idx))
                    s.Order = idx;

            await _db.SaveChangesAsync();
        }

        public async Task<List<RoutineForDayVM>> GetForDateAsync(DateOnly date, string userId)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var routines = await _db.Routines
                .Include(r => r.Steps).ThenInclude(s => s.Products)
                .Include(r => r.Tags)
                .Include(r => r.Entries.Where(e => e.Date == dateUtc))
                .ThenInclude(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.TimeOfDay).ThenBy(r => r.Order)
                .ToListAsync();

            var result = new List<RoutineForDayVM>();

            foreach (var r in routines)
            {
                var todaysSteps = StepsForDate(r, date);
                if (!todaysSteps.Any())
                    continue;

                var entry = r.Entries.FirstOrDefault(e => e.Date == dateUtc);

                var stepEntries = entry?.StepEntries?
                                      .GroupBy(se => se.RoutineStepId)
                                      .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First())
                                  ?? new Dictionary<int, RoutineStepEntry>();

                var vm = new RoutineForDayVM
                {
                    RoutineId = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Color = r.Color,
                    Icon = r.Icon,
                    TimeOfDay = r.TimeOfDay,
                    TotalSteps = todaysSteps.Count,
                    Completed = entry?.Completed ?? false,
                    TagIds = r.Tags?.Select(t => t.Id).ToList() ?? new List<int>()
                };

                int done = 0;

                foreach (var s in todaysSteps)
                {
                    stepEntries.TryGetValue(s.Id, out var se);

                    var stepVm = new RoutineStepForDayVM
                    {
                        StepId = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Action = s.Action,
                        EstimatedMinutes = s.EstimatedMinutes,
                        Completed = se?.Completed ?? false,
                        Skipped = se?.Skipped ?? false,
                        RotationEnabled = s.RotationEnabled,
                        RotationMode = s.RotationMode
                    };

                    var allProducts = (s.Products ?? new List<RoutineStepProduct>())
                        .OrderBy(p => p.Id)
                        .ToList();

                    if (!s.RotationEnabled || allProducts.Count <= 1)
                    {
                        var completedSet = (se?.ProductEntries ?? new List<RoutineStepProductEntry>())
                            .Where(pe => pe.Completed)
                            .Select(pe => pe.RoutineStepProductId)
                            .ToHashSet();

                        stepVm.Products = allProducts.Select(p => new RoutineStepProductForDayVM
                        {
                            ProductId = p.Id,
                            Name = p.Name,
                            Note = p.Note,
                            Url = p.Url,
                            ImageUrl = p.ImageUrl,
                            Completed = completedSet.Contains(p.Id)
                        }).ToList();
                    }
                    else
                    {
                        var mode = (s.RotationMode ?? "ALL").ToUpperInvariant();

                        if (mode == "ALL")
                        {
                            var occIdx = GetOccurrenceIndex(r.StartDateUtc, s.RRule, date);
                            if (occIdx < 0) occIdx = 0;

                            var pick = allProducts[occIdx % allProducts.Count];

                            stepVm.Products = new()
                            {
                                new RoutineStepProductForDayVM
                                {
                                    ProductId = pick.Id,
                                    Name = pick.Name,
                                    Note = pick.Note,
                                    Url = pick.Url,
                                    ImageUrl = pick.ImageUrl,
                                    Completed = false
                                }
                            };
                        }
                        else
                        {
                            var completedSet = (se?.ProductEntries ?? new List<RoutineStepProductEntry>())
                                .Where(pe => pe.Completed)
                                .Select(pe => pe.RoutineStepProductId)
                                .ToHashSet();

                            stepVm.Products = allProducts.Select(p => new RoutineStepProductForDayVM
                            {
                                ProductId = p.Id,
                                Name = p.Name,
                                Note = p.Note,
                                Url = p.Url,
                                ImageUrl = p.ImageUrl,
                                Completed = completedSet.Contains(p.Id)
                            }).ToList();

                            if (se != null)
                            {
                                var anyCompleted = stepVm.Products.Any(pp => pp.Completed);
                                stepVm.Completed = anyCompleted;
                            }
                        }
                    }

                    if (stepVm.Completed || stepVm.Skipped) done++;
                    vm.Steps.Add(stepVm);
                }

                vm.DoneSteps = done;
                result.Add(vm);
            }

            return result;
        }

        public async Task<bool> ToggleStepAsync(int routineId, int stepId, DateOnly date, bool completed, string? note,
            string userId)
        {
            var routine = await _db.Routines
                .Include(r => r.Steps)
                .Include(r => r.Entries.Where(e => e.Date == date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)))
                .ThenInclude(e => e.StepEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { Date = dateUtc, RoutineId = routineId };
                routine.Entries.Add(entry);
            }

            var dupes = entry.StepEntries
                .Where(x => x.RoutineStepId == stepId)
                .OrderByDescending(x => x.Id)
                .ToList();

            var se = dupes.FirstOrDefault();
            foreach (var d in dupes.Skip(1))
                _db.RoutineStepEntries.Remove(d);

            if (se == null)
            {
                se = new RoutineStepEntry { RoutineStepId = stepId };
                entry.StepEntries.Add(se);
            }

            se.Completed = completed;
            if (completed) se.Skipped = false;
            se.Note = note;
            se.CompletedAt = completed ? DateTime.UtcNow : (DateTime?)null;

            entry.Completed = await AreAllScheduledStepsDoneAsync(routine, date);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllStepsAsync(int routineId, DateOnly date, string userId)
        {
            var routine = await _db.Routines
                .Include(r => r.Steps)
                .Include(r => r.Entries.Where(e => e.Date == date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)))
                .ThenInclude(e => e.StepEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { Date = dateUtc, RoutineId = routineId };
                routine.Entries.Add(entry);
            }

            var todaysSteps = StepsForDate(routine, date);

            foreach (var s in todaysSteps)
            {
                var dupes = entry.StepEntries
                    .Where(x => x.RoutineStepId == s.Id)
                    .OrderByDescending(x => x.Id)
                    .ToList();

                var se = dupes.FirstOrDefault();
                foreach (var d in dupes.Skip(1))
                    _db.RoutineStepEntries.Remove(d);

                if (se == null)
                {
                    se = new RoutineStepEntry { RoutineStepId = s.Id };
                    entry.StepEntries.Add(se);
                }

                se.Completed = true;
                se.Skipped = false;
                se.CompletedAt = DateTime.UtcNow;
            }

            entry.Completed = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkRoutineCompletedAsync(int routineId, DateOnly date, string userId)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var routine = await _db.Routines
                .Include(r => r.Entries.Where(e => e.Date == dateUtc))
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { RoutineId = routineId, Date = dateUtc };
                _db.RoutineEntries.Add(entry);
            }

            entry.Completed = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<bool> AreAllScheduledStepsDoneAsync(Routine routine, DateOnly date)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null) return false;

            var allSteps = await _db.RoutineSteps
                .Where(s => s.RoutineId == routine.Id)
                .Select(s => new { s.Id, s.RRule })
                .ToListAsync();

            var todaysStepIds = allSteps
                .Where(s => OccursOnDate(routine.StartDateUtc, s.RRule, date))
                .Select(s => s.Id)
                .ToList();

            if (todaysStepIds.Count == 0) return false;

            var map = entry.StepEntries
                .GroupBy(se => se.RoutineStepId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            foreach (var stepId in todaysStepIds)
            {
                if (!map.TryGetValue(stepId, out var se) || !(se.Completed || se.Skipped))
                    return false;
            }

            return true;
        }

        private static bool OccursOnDate(DateTime startUtc, string? rrule, DateOnly date)
            => OccursOnDateCore(startUtc, rrule, date, ignoreCount: false);

        private static bool OccursOnDateCore(DateTime startUtc, string? rrule, DateOnly date, bool ignoreCount)
        {
            var day = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
            var start = startUtc.Date;
            if (day < start) return false;

            if (string.IsNullOrWhiteSpace(rrule))
                return true;

            var map = ParseRRule(rrule);

            if (map.TryGetValue("UNTIL", out var untilRaw))
            {
                if (TryParseUntil(untilRaw, out var untilDate) && day > untilDate)
                    return false;
            }

            var freq = map.TryGetValue("FREQ", out var f) ? f : "DAILY";
            var interval = map.TryGetValue("INTERVAL", out var i) && int.TryParse(i, out var iv) ? Math.Max(1, iv) : 1;

            bool match;
            switch (freq)
            {
                case "DAILY":
                {
                    var days = (int)(day - start).TotalDays;
                    match = days % interval == 0;
                    break;
                }
                case "WEEKLY":
                {
                    var byday = map.TryGetValue("BYDAY", out var bd)
                        ? bd.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToHashSet()
                        : new HashSet<string> { ToByDay(start.DayOfWeek) };

                    var weeks = WeeksBetween(start, day);
                    match = weeks % interval == 0 && byday.Contains(ToByDay(day.DayOfWeek));
                    break;
                }
                case "MONTHLY":
                {
                    var bymd = map.TryGetValue("BYMONTHDAY", out var md)
                        ? md.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(int.Parse).ToHashSet()
                        : new HashSet<int> { start.Day };

                    var months = (day.Year - start.Year) * 12 + (day.Month - start.Month);
                    match = months % interval == 0 && bymd.Contains(day.Day);
                    break;
                }
                default:
                    match = false;
                    break;
            }

            if (!match) return false;

            if (!ignoreCount && map.TryGetValue("COUNT", out var cntRaw) && int.TryParse(cntRaw, out var cnt) &&
                cnt > 0)
            {
                var occIdx = GetOccurrenceIndexCore(startUtc, rrule, date);
                return occIdx >= 0 && occIdx < cnt;
            }

            return true;
        }

        public async Task<bool> SetAllStepsAsync(int routineId, DateOnly date, bool completed, string userId)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var routine = await _db.Routines
                .Include(r => r.Steps).ThenInclude(s => s.Products)
                .Include(r => r.Entries.Where(e => e.Date == dateUtc))
                .ThenInclude(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { RoutineId = routineId, Date = dateUtc };
                routine.Entries.Add(entry);
            }

            var todaysSteps = StepsForDate(routine, date);

            foreach (var s in todaysSteps)
            {
                var se = entry.StepEntries.FirstOrDefault(x => x.RoutineStepId == s.Id);
                if (se == null)
                {
                    se = new RoutineStepEntry { RoutineStepId = s.Id };
                    entry.StepEntries.Add(se);
                }

                se.Completed = completed;
                se.Skipped = false;
                se.CompletedAt = completed ? DateTime.UtcNow : (DateTime?)null;

                if (s.RotationEnabled && string.Equals(s.RotationMode, "ANY", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var p in s.Products ?? Enumerable.Empty<RoutineStepProduct>())
                    {
                        var pe = se.ProductEntries.FirstOrDefault(x => x.RoutineStepProductId == p.Id);
                        if (pe == null)
                        {
                            pe = new RoutineStepProductEntry { RoutineStepProductId = p.Id };
                            se.ProductEntries.Add(pe);
                        }

                        pe.Completed = completed;
                        pe.CompletedAt = completed ? DateTime.UtcNow : (DateTime?)null;
                    }
                }
            }

            entry.Completed = completed;

            await _db.SaveChangesAsync();
            return true;
        }


        public async Task<bool> SetRoutineCompletedAsync(int routineId, DateOnly date, bool completed, string userId)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var routine = await _db.Routines
                .Include(r => r.Entries.Where(e => e.Date == dateUtc))
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { RoutineId = routineId, Date = dateUtc };
                _db.RoutineEntries.Add(entry);
            }

            entry.Completed = completed;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleStepProductAsync(
            int routineId, int stepId, int productId, DateOnly date, bool completed, string userId)
        {
            var dateUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var routine = await _db.Routines
                .Include(r => r.Entries.Where(e => e.Date == dateUtc))
                .ThenInclude(e => e.StepEntries)
                .ThenInclude(se => se.ProductEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return false;

            var step = await _db.RoutineSteps
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == stepId && s.RoutineId == routineId);

            if (step == null) return false;

            var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
            if (entry == null)
            {
                entry = new RoutineEntry { RoutineId = routine.Id, Date = dateUtc };
                routine.Entries.Add(entry);
            }

            var se = entry.StepEntries.FirstOrDefault(x => x.RoutineStepId == stepId);
            if (se == null)
            {
                se = new RoutineStepEntry { RoutineStepId = stepId };
                entry.StepEntries.Add(se);
            }

            var pe = se.ProductEntries.FirstOrDefault(x => x.RoutineStepProductId == productId);
            if (pe == null)
            {
                pe = new RoutineStepProductEntry { RoutineStepProductId = productId };
                se.ProductEntries.Add(pe);
            }

            pe.Completed = completed;
            pe.CompletedAt = completed ? DateTime.UtcNow : null;

            if (step.RotationEnabled && string.Equals(step.RotationMode, "ANY", StringComparison.OrdinalIgnoreCase))
            {
                se.Completed = se.ProductEntries.Any(x => x.Completed);
                if (!se.Completed) se.Skipped = false;
                se.CompletedAt = se.Completed ? DateTime.UtcNow : null;
            }

            entry.Completed = await AreAllScheduledStepsDoneAsync(routine, date);

            await _db.SaveChangesAsync();
            return true;
        }

        private static int GetOccurrenceIndex(DateTime startUtc, string? rrule, DateOnly date)
            => GetOccurrenceIndexCore(startUtc, rrule, date);

        private static int GetOccurrenceIndexCore(DateTime startUtc, string? rrule, DateOnly date)
        {
            var target = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
            int idx = 0;

            for (var d = startUtc.Date; d <= target; d = d.AddDays(1))
            {
                if (OccursOnDateCore(startUtc, rrule, DateOnly.FromDateTime(d), ignoreCount: true))
                {
                    if (d == target) return idx;
                    idx++;
                }
            }

            return -1;
        }

        private static bool TryParseUntil(string raw, out DateTime untilDate)
        {
            var s = raw.Trim();

            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
            {
                untilDate = d1.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
                return true;
            }

            if (DateTime.TryParseExact(s, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dtZ))
            {
                untilDate = dtZ.Date;
                return true;
            }

            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dtShort))
            {
                untilDate = dtShort.Date;
                return true;
            }

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var any))
            {
                untilDate = any.Date;
                return true;
            }

            untilDate = DateTime.MaxValue.Date;
            return false;
        }

        private static Dictionary<string, string> ParseRRule(string rrule)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = rrule.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                var idx = p.IndexOf('=');
                if (idx <= 0) continue;
                var key = p[..idx].ToUpperInvariant();
                var val = p[(idx + 1)..].ToUpperInvariant();
                dict[key] = val;
            }

            return dict;
        }

        private static string ToByDay(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => "MO",
            DayOfWeek.Tuesday => "TU",
            DayOfWeek.Wednesday => "WE",
            DayOfWeek.Thursday => "TH",
            DayOfWeek.Friday => "FR",
            DayOfWeek.Saturday => "SA",
            DayOfWeek.Sunday => "SU",
            _ => "MO"
        };

        private static int WeeksBetween(DateTime start, DateTime day)
        {
            int startOffset = ((int)start.DayOfWeek + 6) % 7;
            int dayOffset = ((int)day.DayOfWeek + 6) % 7;

            var startMonday = start.AddDays(-startOffset).Date;
            var dayMonday = day.AddDays(-dayOffset).Date;

            return (int)((dayMonday - startMonday).TotalDays / 7.0);
        }

        public async Task<RoutineStatsVM> GetRoutineStatsAsync(int routineId, string userId)
        {
            var routine = await _db.Routines
                .Include(r => r.Steps).ThenInclude(s => s.Products)
                .Include(r => r.Entries).ThenInclude(e => e.StepEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) throw new KeyNotFoundException();

            var start = routine.StartDateUtc.Date;
            var today = DateTime.UtcNow.Date;

            var stepsUntil = GetStepsMaxUntilUtc(routine)?.Date;
            var end = (stepsUntil.HasValue && stepsUntil.Value < today) ? stepsUntil.Value : today;

            int totalOcc = 0, completedDays = 0, partialDays = 0, skippedDays = 0;
            int currentStreak = 0, bestStreak = 0;

            var entriesMap = routine.Entries
                .GroupBy(e => e.Date.Date)
                .ToDictionary(g => g.Key, g => g.First());

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var dOnly = DateOnly.FromDateTime(day);

                var todaysSteps = StepsForDate(routine, dOnly);
                if (todaysSteps.Count == 0)
                    continue;

                totalOcc++;

                entriesMap.TryGetValue(day, out var entry);
                var stepMap = entry?.StepEntries
                                  .GroupBy(se => se.RoutineStepId)
                                  .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First())
                              ?? new Dictionary<int, RoutineStepEntry>();

                int done = 0, skipped = 0;
                foreach (var s in todaysSteps)
                {
                    if (stepMap.TryGetValue(s.Id, out var se))
                    {
                        if (se.Completed) done++;
                        else if (se.Skipped) skipped++;
                    }
                }

                if (done == todaysSteps.Count)
                {
                    completedDays++;
                    currentStreak++;
                    bestStreak = Math.Max(bestStreak, currentStreak);
                }
                else if (done > 0 || skipped > 0)
                {
                    partialDays++;
                    currentStreak = 0;
                }
                else
                {
                    skippedDays++;
                    currentStreak = 0;
                }
            }

            var stats = new RoutineStatsVM
            {
                StartDateUtc = routine.StartDateUtc,
                EndDateUtc = stepsUntil,
                TotalOccurrences = totalOcc,
                CompletedDays = completedDays,
                PartialDays = partialDays,
                SkippedDays = skippedDays,
                CurrentStreak = currentStreak,
                BestStreak = bestStreak,
                OverallPercent = totalOcc > 0 ? (double)completedDays / totalOcc * 100.0 : 0.0
            };

            var stepAgg = new Dictionary<int, (int skip, int comp, int total)>();
            foreach (var s in routine.Steps)
                stepAgg[s.Id] = (0, 0, 0);

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var dOnly = DateOnly.FromDateTime(day);

                foreach (var step in routine.Steps)
                {
                    if (!OccursOnDate(routine.StartDateUtc, step.RRule, dOnly))
                        continue;

                    var agg = stepAgg[step.Id];
                    agg.total++;

                    entriesMap.TryGetValue(day, out var entry);
                    RoutineStepEntry? se = null;

                    if (entry != null)
                    {
                        se = entry.StepEntries
                            .Where(x => x.RoutineStepId == step.Id)
                            .OrderByDescending(x => x.Id)
                            .FirstOrDefault();
                    }

                    if (se?.Completed == true)
                    {
                        agg.comp++;
                    }
                    else if (se?.Skipped == true)
                    {
                        agg.skip++;
                    }
                    else
                    {
                        if (day < today)
                            agg.skip++;
                    }

                    stepAgg[step.Id] = agg;
                }
            }

            stats.TopSkippedSteps = routine.Steps
                .Select(s => new StepSkipVM
                {
                    StepId = s.Id,
                    Name = s.Name,
                    SkippedCount = stepAgg.TryGetValue(s.Id, out var v) ? v.skip : 0,
                    CompletedCount = stepAgg.TryGetValue(s.Id, out var v2) ? v2.comp : 0,
                    TotalEntries = stepAgg.TryGetValue(s.Id, out var v3) ? v3.total : 0
                })
                .Where(x => x.SkippedCount > 0)
                .OrderByDescending(x => x.SkippedCount)
                .ThenBy(x => x.Name)
                .Take(5)
                .ToList();

            return stats;
        }


        public async Task<List<RoutineDayEntryVM>> GetRoutineEntriesAsync(int routineId, DateOnly from, DateOnly to,
            string userId)
        {
            var routine = await _db.Routines
                .Include(r => r.Steps)
                .Include(r => r.Entries.Where(e => e.Date >= from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                                                   && e.Date <= to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)))
                .ThenInclude(e => e.StepEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);
            if (routine == null) throw new KeyNotFoundException();

            var list = new List<RoutineDayEntryVM>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var todaysSteps = StepsForDate(routine, d);
                if (todaysSteps.Count == 0) continue;

                var dateUtc = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var entry = routine.Entries.FirstOrDefault(e => e.Date == dateUtc);
                var stepMap = entry?.StepEntries.GroupBy(se => se.RoutineStepId)
                                  .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First())
                              ?? new Dictionary<int, RoutineStepEntry>();

                int done = 0;
                foreach (var s in todaysSteps)
                    if (stepMap.TryGetValue(s.Id, out var se) && se.Completed)
                        done++;

                list.Add(new RoutineDayEntryVM
                    { Date = d.ToString("yyyy-MM-dd"), CompletedSteps = done, TotalSteps = todaysSteps.Count });
            }

            return list;
        }

        public async Task<Dictionary<string, string>> GetRoutineMonthMapAsync(int routineId, int year, int month,
            string userId)
        {
            var todayUtcDate = DateTime.UtcNow.Date;

            var routine = await _db.Routines
                .Include(r => r.Steps)
                .Include(r => r.Entries.Where(e => e.Date.Year == year && e.Date.Month == month))
                .ThenInclude(e => e.StepEntries)
                .FirstOrDefaultAsync(r => r.Id == routineId && r.UserId == userId);

            if (routine == null) return new Dictionary<string, string>();

            var steps = routine.Steps.Select(s => new { s.Id, s.RRule }).ToList();

            var result = new Dictionary<string, string>();
            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var dUtc = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                var d = DateOnly.FromDateTime(dUtc);

                var key = $"{year}-{month:00}-{day:00}";

                bool outOfRange = dUtc.Date < routine.StartDateUtc.Date;
                if (outOfRange)
                {
                    result[key] = "off";
                    continue;
                }

                var todaysStepIds = steps
                    .Where(s => OccursOnDate(routine.StartDateUtc, s.RRule, d))
                    .Select(s => s.Id)
                    .ToList();

                if (todaysStepIds.Count == 0)
                {
                    result[key] = "off";
                    continue;
                }

                var entry = routine.Entries.FirstOrDefault(e => e.Date.Date == dUtc.Date);
                if (entry == null)
                {
                    if (dUtc.Date > todayUtcDate)
                    {
                        result[key] = "future";
                        continue;
                    }

                    result[key] = "none";
                    continue;
                }

                if (entry.Completed)
                {
                    result[key] = "full";
                    continue;
                }

                var map = entry.StepEntries
                    .GroupBy(se => se.RoutineStepId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

                bool anyTouched = todaysStepIds.Any(id =>
                    map.TryGetValue(id, out var se) && (se.Completed || se.Skipped));

                result[key] = anyTouched ? "partial" : "none";
            }

            return result;
        }

        public Task<List<TagVM>> GetUserTagsAsync(string userId)
        {
            return _db.Tags
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .Select(t => new TagVM { Id = t.Id, Name = t.Name })
                .ToListAsync();
        }

        private static bool AnyStepOccursOnDate(Routine routine, DateOnly date)
        {
            foreach (var s in routine.Steps)
            {
                if (OccursOnDate(routine.StartDateUtc, s.RRule, date))
                    return true;
            }

            return false;
        }

        private static List<RoutineStep> StepsForDate(Routine routine, DateOnly date)
        {
            return routine.Steps
                .Where(s => OccursOnDate(routine.StartDateUtc, s.RRule, date))
                .OrderBy(s => s.Order)
                .ToList();
        }

        private static DateTime? GetStepsMaxUntilUtc(Routine routine)
        {
            DateTime? max = null;
            foreach (var s in routine.Steps)
            {
                if (string.IsNullOrWhiteSpace(s.RRule)) continue;
                var map = ParseRRule(s.RRule);
                if (map.TryGetValue("UNTIL", out var raw) && TryParseUntil(raw, out var u))
                    max = (max == null || u > max.Value) ? u : max;
            }

            return max;
        }
    }
}