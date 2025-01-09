using Blog.Models;
using Blog.Repositories.Interfaces;
using Blog.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UsersDbContext _context;

        public UserRepository(UsersDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получение всех пользователей.
        /// </summary>
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Добавление нового пользователя.
        /// </summary>
        public async Task AddUserAsync(UserModel user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Получение пользователя по email.
        /// </summary>
        public async Task<UserModel> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Проверка существования пользователя.
        /// </summary>
        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        
        public async Task UpdateUserAsync(UserModel user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

    }
}