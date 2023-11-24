using Microsoft.AspNetCore.Mvc;
using ConsistentHashingRedis.Domain.ConsistentHashing;

namespace ConsistentHashingRedis.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisController : ControllerBase
{
    private readonly IConsistentHashing _consistentHashing;

    public RedisController(IConsistentHashing consistentHashing)
    {
        _consistentHashing = consistentHashing;
    }

    [HttpPost("AddServer/{name}/{host}")]
    public IActionResult AddServer(string name, string host)
    {
        var key = _consistentHashing.AddServer(name, host);
        return Ok(new { key });
    }

    [HttpDelete("AddServer/{name}/{host}")]
    public IActionResult RemoveServer(string name, string host)
    {
        _consistentHashing.RemoveServer(name, host);
        return Ok(new { ok = true });
    }

    [HttpGet("{name}")]
    public IActionResult Get(string name)
    {
        var redisServer = _consistentHashing.GetInstance(name);
        // espaco para usar o redis
        // redisServer.Connection.GetDatabase().StringGetAsync(..)
        // redisServer.Connection.GetDatabase().StringSetAsync(...)
        return Ok(new { redisName = redisServer?.Name ?? "" });
    }

}

