using Blog.Repositories.Interfaces;

namespace Blog.Repositories;

public class IdempotencyKeysRepository : IIdempotencyKeysRepository
{
    private static readonly HashSet<string> _idempotencyKeys = new();
    
    public bool Contains(string key)
    {
        return _idempotencyKeys.Contains(key);
    }
    
    public bool Add(string key)
    {
        return _idempotencyKeys.Add(key);
    }
}