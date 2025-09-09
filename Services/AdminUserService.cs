using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels.AdminUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly LifeCareDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public const string AdminRole = "Admin";

        public AdminUserService(LifeCareDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<AdminUserListItemVM>> ListAsync()
        {
            var users = await _db.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
            var list = new List<AdminUserListItemVM>();
            foreach (var u in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(u, AdminRole);
                list.Add(new AdminUserListItemVM
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    DisplayName = u.DisplayName ?? "",
                    IsAdmin = isAdmin || u.IsAdmin
                });
            }
            return list;
        }

        public async Task<AdminUserDetailsVM?> GetAsync(string id)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return null;
            var isAdmin = await _userManager.IsInRoleAsync(u, AdminRole);
            return new AdminUserDetailsVM
            {
                Id = u.Id,
                Email = u.Email ?? "",
                DisplayName = u.DisplayName ?? "",
                IsAdmin = isAdmin || u.IsAdmin
            };
        }

        public async Task<AdminUserEditVM?> GetForEditAsync(string id)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return null;
            var isAdmin = await _userManager.IsInRoleAsync(u, AdminRole);
            return new AdminUserEditVM
            {
                Id = u.Id,
                Email = u.Email ?? "",
                DisplayName = u.DisplayName ?? "",
                IsAdmin = isAdmin || u.IsAdmin
            };
        }

        public async Task<(bool ok, string? error, string? id)> CreateAsync(AdminUserCreateVM vm)
        {
            var exists = await _userManager.FindByEmailAsync(vm.Email);
            if (exists != null) return (false, "Email already in use", null);

            var u = new User
            {
                UserName = vm.Email,
                Email = vm.Email,
                DisplayName = vm.DisplayName,
                IsAdmin = vm.IsAdmin
            };

            var createRes = await _userManager.CreateAsync(u, vm.Password);
            if (!createRes.Succeeded) return (false, string.Join("; ", createRes.Errors.Select(e => e.Description)), null);

            await EnsureAdminRoleExists();
            var inRole = await _userManager.IsInRoleAsync(u, AdminRole);
            if (vm.IsAdmin && !inRole) await _userManager.AddToRoleAsync(u, AdminRole);
            if (!vm.IsAdmin && inRole) await _userManager.RemoveFromRoleAsync(u, AdminRole);

            if (_db.Entry(u).State == EntityState.Detached)
            {
                var dbUser = await _db.Users.FirstAsync(x => x.Id == u.Id);
                dbUser.IsAdmin = vm.IsAdmin;
                await _db.SaveChangesAsync();
            }

            return (true, null, u.Id);
        }

        public async Task<(bool ok, string? error)> UpdateAsync(AdminUserEditVM vm)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (u == null) return (false, "Not found");

            var emailOwner = await _userManager.FindByEmailAsync(vm.Email);
            if (emailOwner != null && emailOwner.Id != u.Id) return (false, "Email already in use");

            u.Email = vm.Email;
            u.UserName = vm.Email;
            u.DisplayName = vm.DisplayName;
            u.IsAdmin = vm.IsAdmin;

            var upd = await _userManager.UpdateAsync(u);
            if (!upd.Succeeded) return (false, string.Join("; ", upd.Errors.Select(e => e.Description)));

            await EnsureAdminRoleExists();
            var inRole = await _userManager.IsInRoleAsync(u, AdminRole);
            if (vm.IsAdmin && !inRole) await _userManager.AddToRoleAsync(u, AdminRole);
            if (!vm.IsAdmin && inRole) await _userManager.RemoveFromRoleAsync(u, AdminRole);

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(u);
                var reset = await _userManager.ResetPasswordAsync(u, token, vm.NewPassword);
                if (!reset.Succeeded) return (false, string.Join("; ", reset.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteAsync(string id)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return (false, "Not found");

            var isAdmin = u.IsAdmin || await _userManager.IsInRoleAsync(u, AdminRole);
            if (isAdmin)
            {
                var adminsInRole = await _userManager.GetUsersInRoleAsync(AdminRole);
                var anyOtherAdmin =
                    adminsInRole.Any(x => x.Id != id) ||
                    await _db.Users.AnyAsync(x => x.Id != id && x.IsAdmin);

                if (!anyOtherAdmin)
                    return (false, "Cannot delete the last admin");
            }

            await DeleteUserDataAsync(u.Id);

            var roles = await _userManager.GetRolesAsync(u);
            if (roles.Count > 0)
                await _userManager.RemoveFromRolesAsync(u, roles);

            var res = await _userManager.DeleteAsync(u);
            if (!res.Succeeded)
                return (false, string.Join("; ", res.Errors.Select(e => e.Description)));

            return (true, null);
        }

        private async Task EnsureAdminRoleExists()
        {
            if (!await _roleManager.RoleExistsAsync(AdminRole))
                await _roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        private async Task DeleteUserDataAsync(string userId)
        {
            var routineEntries = await _db.RoutineEntries
                .Where(e => e.Routine.UserId == userId)
                .Include(e => e.StepEntries).ThenInclude(se => se.ProductEntries)
                .ToListAsync();

            foreach (var e in routineEntries)
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

            var steps = await _db.RoutineSteps
                .Where(s => s.Routine.UserId == userId)
                .Include(s => s.Products)
                .ToListAsync();

            foreach (var s in steps)
            {
                if (s.Products != null && s.Products.Count > 0)
                    _db.RemoveRange(s.Products);
                _db.Remove(s);
            }
            await _db.SaveChangesAsync();

            var routines = await _db.Routines.Where(r => r.UserId == userId).ToListAsync();
            if (routines.Count > 0)
            {
                _db.RemoveRange(routines);
                await _db.SaveChangesAsync();
            }

            var habitEntries = await _db.HabitEntries.Where(e => e.Habit.UserId == userId).ToListAsync();
            if (habitEntries.Count > 0)
            {
                _db.RemoveRange(habitEntries);
                await _db.SaveChangesAsync();
            }

            var habits = await _db.Habits.Where(h => h.UserId == userId).Include(h => h.Tags).ToListAsync();
            foreach (var h in habits)
                h.Tags?.Clear();
            if (habits.Count > 0)
            {
                _db.RemoveRange(habits);
                await _db.SaveChangesAsync();
            }

            var tags = await _db.Tags.Where(t => t.UserId == userId).ToListAsync();
            if (tags.Count > 0)
            {
                _db.RemoveRange(tags);
                await _db.SaveChangesAsync();
            }

            var nplans = await _db.NutritionPlans.Where(p => p.UserId == userId).ToListAsync();
            if (nplans.Count > 0)
            {
                _db.RemoveRange(nplans);
                await _db.SaveChangesAsync();
            }

            var wplans = await _db.WorkoutPlans.Where(p => p.UserId == userId).ToListAsync();
            if (wplans.Count > 0)
            {
                _db.RemoveRange(wplans);
                await _db.SaveChangesAsync();
            }
        }
    }
}
