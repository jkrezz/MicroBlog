using Blog.Models;
using System.Collections.Generic;

namespace Blog.Repositories.Interfaces;
public interface IUserRepository
{
    List<UserModel> GetAllUsers();          // Получение всех пользователей
    void AddUser(UserModel user);           // Добавление пользователя
    UserModel GetUserByEmail(string email); // Получение пользователя по email
    bool UserExists(string email);          // Проверка, существует ли пользователь
}
