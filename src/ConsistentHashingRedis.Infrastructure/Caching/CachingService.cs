
using ConsistentHashingRedis.Domain.Caching;

namespace ConsistentHashingRedis.Infrastructure.Caching;

public class CachingService : ICachingService
{
    public Task<string> GetAsync(string ke)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, string value)
    {
        throw new NotImplementedException();
    }
}