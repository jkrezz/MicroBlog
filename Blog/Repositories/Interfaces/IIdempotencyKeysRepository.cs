namespace Blog.Repositories.Interfaces;

public interface IIdempotencyKeysRepository
{
    bool Contains(string key);
    
    bool Add(string key);
}