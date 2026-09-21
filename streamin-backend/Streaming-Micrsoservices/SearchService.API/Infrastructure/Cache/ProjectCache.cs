using Microsoft.EntityFrameworkCore;
using SearchService.API.Infrastructure.Projections;
using SearchService.API.Infrastructure.Projections.Models;
using System.Collections.Concurrent;

namespace SearchService.API.Infrastructure.Cache
{
    public sealed class ProjectCache
    {
       // private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        public ProjectCache(IServiceScopeFactory serviceScopeFactory)
        {
            _scopeFactory= serviceScopeFactory;
        }
        private readonly ConcurrentDictionary<Guid, ProjectProjection> _cache = new();

        public ValueTask<ProjectProjection?> GetValue(Guid projectId)
        {
            ProjectProjection? projection= null;
            bool res= _cache.TryGetValue(projectId,out projection);
            if (res)
            {
                return new ValueTask<ProjectProjection?>(projection);
            }
            return new ValueTask<ProjectProjection?>(getFromDB(projectId));
            
        }
        private async Task<ProjectProjection?> getFromDB(Guid projectId)
        {
            ProjectProjection? projection= null;
            using var scope = _scopeFactory.CreateScope();
            using AppDbContext _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = _context.ProjectProjections.Where(x => x.ProjectId == projectId).AsNoTracking();
            if (await query.AnyAsync())
            {
                projection = await query.FirstOrDefaultAsync();
                _cache.TryAdd(projectId, projection);
               
            }
            return projection;
        }

        public void Insert(Guid projectId, ProjectProjection projection) {
            var check=_cache.TryAdd(projectId, projection);
            if (check!)
            {
                _cache.TryRemove(projectId,out _);
                _cache.TryAdd(projectId, projection);
            }
        }
        public bool Remove(Guid projectId) {
            return _cache.TryRemove(projectId, out _);
        }

    }
}
