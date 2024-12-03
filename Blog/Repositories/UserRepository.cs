using Blog.Models;
using Blog.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Blog.Repositories.Interfaces;

namespace Blog.Repositories;

public class UserRepository : IUserRepository
{
    private static List<UserModel> _users = new();

    // Метод для получения всех пользователей
    public List<UserModel> GetAllUsers()
    {
        return _users;
    }

    // Метод для добавления нового пользователя
    public void AddUser(UserModel user)
    {
        _users.Add(user);
    }

    // Метод для получения пользователя по email
    public UserModel GetUserByEmail(string email)
    {
        return _users.FirstOrDefault(u => u.Email == email);
    }

    // Метод для проверки существования пользователя
    public bool UserExists(string email)
    {
        return _users.Any(u => u.Email == email);
    }
}
