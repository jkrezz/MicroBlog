using Blog.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserModel>> GetAllUsersAsync();        // Получение всех пользователей
        Task AddUserAsync(UserModel user);              // Добавление пользователя
        Task<UserModel> GetUserByEmailAsync(string email); // Получение пользователя по email
        Task<bool> UserExistsAsync(string email);       // Проверка, существует ли пользователь
        Task UpdateUserAsync(UserModel user);
    }
}