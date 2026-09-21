using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SearchService.API.Infrastructure.Projections;
using SearchService.API.Infrastructure.Projections.Models;
using System.Collections.Concurrent;

namespace SearchService.API.Infrastructure.Cache
{
    public class ProjectUserCache
    {
        //private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        public ProjectUserCache(IServiceScopeFactory serviceScopeFactory)
        {
            _scopeFactory= serviceScopeFactory;
        }
        private readonly ConcurrentDictionary<Guid, ProjectUserProjection> _cache = new();

        public ValueTask<ProjectUserProjection?> GetValue(Guid projectUserId)
        {
            ProjectUserProjection? projection = null;
            bool res = _cache.TryGetValue(projectUserId, out projection);
            if (res)
            {
                return new ValueTask<ProjectUserProjection?>(projection);
            }

            return new ValueTask<ProjectUserProjection?>(GetFromDb(projectUserId));
        }
        private async Task<ProjectUserProjection?> GetFromDb(Guid projectUserId)
        {
            ProjectUserProjection? projection = null;
            using var scope = _scopeFactory.CreateScope();
            using AppDbContext _context=scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = _context.ProjectUserProjections.Where(x => x.UserId == projectUserId).AsNoTracking();
            if (await query.AnyAsync())
            {
                projection = await query.FirstOrDefaultAsync();
                _cache.TryAdd(projectUserId, projection);
            }
            return projection;
        }

        public void Insert(Guid projectUserId, ProjectUserProjection projection)
        {
            bool res=_cache.TryAdd(projectUserId, projection);
            if (!res)
            {
                _cache.TryRemove(projectUserId, out _);
                _cache.TryAdd(projectUserId, projection);
            }
        }
        public bool Remove(Guid projectUserId)
        {
            return _cache.TryRemove(projectUserId, out _);
        }
    }
}
