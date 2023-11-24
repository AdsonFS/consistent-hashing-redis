using System.Security.Cryptography;
using System.Text;
using ConsistentHashingRedis.Domain.ConsistentHashing;
using ConsistentHashingRedis.Domain.Entity;
using StackExchange.Redis;

namespace ConsistentHashingRedis.Infrastructure.ConsistentHashing;

public class ConsistentHashing : IConsistentHashing
{
    private readonly uint[] _tree;
    private readonly MD5 _md5Hasher;
    private readonly uint _hashRange = 100;
    private readonly Dictionary<uint, RedisServer> _map = new();
    private static readonly SemaphoreSlim _semaphoreSlim = new(1);
    public ConsistentHashing()
    {
        _md5Hasher = MD5.Create();
        _tree = new uint[_hashRange * 4];
    }
    private uint GetHash(string key)
    {
        var hashed = _md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(key));
        return (BitConverter.ToUInt32(hashed, 0) % _hashRange) + 1;
    }

    public uint AddServer(string name, string host)
    {
        _semaphoreSlim.Wait();
        uint hashKey = GetHash(host);
        System.Console.WriteLine(hashKey);
        if (_map.ContainsKey(hashKey))
        {
            System.Console.WriteLine("Houve conflito no hash");
            _semaphoreSlim.Release();
            return 0;
        }

        System.Console.WriteLine($"Servidor: {name}\tKey: {hashKey}");
        _map.Add(hashKey, new(name, host));
        UpdateSegTree(1, 1, _hashRange, hashKey, hashKey);
        _semaphoreSlim.Release();
        return hashKey;

    }
    public void RemoveServer(string name, string host)
    {
        _semaphoreSlim.Wait();
        uint hashKey = GetHash(host);
        System.Console.WriteLine(hashKey);
        if (_map.ContainsKey(hashKey) && _map[hashKey].Host == host)
        {
            _map[hashKey].Connection.Dispose();
            _map.Remove(hashKey);
            UpdateSegTree(1, 1, _hashRange, hashKey, 0);
            System.Console.WriteLine($"Servidor: {name}\tKey: {hashKey}");
            _semaphoreSlim.Release();

            return;
        }
        _semaphoreSlim.Release();
    }
    public RedisServer? GetInstance(string name)
    {
        if (_map.Count == 0) return null;
        uint hashKey = GetHash(name);
        System.Console.WriteLine(hashKey);

        uint index = QuerySegTree(1, 1, _hashRange, hashKey, _hashRange);
        if (index == 0) index = QuerySegTree(1, 1, _hashRange, 1, hashKey - 1);
        System.Console.WriteLine($"Servidor Selecionado: {_map[index].Name}");
        return _map[index];
    }
    private uint MergeNodes(uint leftValue, uint rightValue)
    {
        if (leftValue > 0) return leftValue;
        return rightValue;
    }
    private void UpdateSegTree(uint no, uint lRange, uint rRange, uint index, uint value)
    {
        if (lRange > index || rRange < index) return;
        if (lRange == rRange) { _tree[no] = value; return; }
        uint leftNo = no * 2, rightNo = no * 2 + 1, mid = (lRange + rRange) / 2;
        UpdateSegTree(leftNo, lRange, mid, index, value);
        UpdateSegTree(rightNo, mid + 1, rRange, index, value);

        _tree[no] = MergeNodes(_tree[leftNo], _tree[rightNo]);
    }
    private uint QuerySegTree(uint no, uint lRange, uint rRange, uint lQuery, uint rQuery)
    {
        if (lRange > rQuery || rRange < lQuery) return 0;
        if (lRange >= lQuery && rRange <= rQuery) return _tree[no];
        uint leftNo = no * 2, rightNo = no * 2 + 1, mid = (lRange + rRange) / 2;

        uint leftValue = QuerySegTree(leftNo, lRange, mid, lQuery, rQuery);
        uint rightValue = QuerySegTree(rightNo, mid + 1, rRange, lQuery, rQuery);

        return MergeNodes(leftValue, rightValue);
    }
}
