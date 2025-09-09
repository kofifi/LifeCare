using AutoMapper;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Services
{
    public class TagService : ITagService
    {
        private readonly LifeCareDbContext _db;
        private readonly IMapper _mapper;

        public TagService(LifeCareDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TagVM>> GetUserTagsAsync(string userId)
        {
            var tags = await _db.Tags
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return _mapper.Map<List<TagVM>>(tags);
        }

        public async Task<TagVM?> GetByIdAsync(int id, string userId)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            return tag == null ? null : _mapper.Map<TagVM>(tag);
        }

        public async Task<TagVM> CreateTagAsync(string name, string userId)
        {
            var tag = new Tag { Name = name, UserId = userId };
            _db.Tags.Add(tag);
            await _db.SaveChangesAsync();
            return _mapper.Map<TagVM>(tag);
        }

        public async Task<bool> UpdateTagAsync(int id, string name, string userId)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (tag == null) return false;

            tag.Name = name;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTagAsync(int id, string userId)
        {
            var tag = await _db.Tags
                .Include(t => t.Habits)
                .Include(t => t.Routines)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tag == null) return false;

            tag.Habits.Clear();
            tag.Routines.Clear();
            await _db.SaveChangesAsync();

            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}