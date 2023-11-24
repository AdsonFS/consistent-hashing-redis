using StackExchange.Redis;

namespace ConsistentHashingRedis.Domain.Entity;

public class RedisServer
{
    public readonly string Name;
    public readonly string Host;
    public readonly ConnectionMultiplexer Connection;

    public RedisServer(string name, string host)
    {
        Name = name;
        Host = host;
        Connection = ConnectionMultiplexer.Connect(host);
    }
}
