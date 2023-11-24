using ConsistentHashingRedis.Domain.Entity;
using StackExchange.Redis;

namespace ConsistentHashingRedis.Domain.ConsistentHashing;
public interface IConsistentHashing
{
    uint AddServer(string name, string host);
    void RemoveServer(string name, string host);
    RedisServer? GetInstance(string name);

}