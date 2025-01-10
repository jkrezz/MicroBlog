using Blog.Data;
using Blog.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;

namespace Blog.Tests
{
    public class TestUsersDbContext : UsersDbContext
    {
        public TestUsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options, null)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
           
        }
    }

    [TestFixture]
    public class UsersDbContextTests
    {
        private DbContextOptions<UsersDbContext> _options;
        private UsersDbContext _context;

        [SetUp]
        public void SetUp()
        {
            // Создаем базу данных в памяти с уникальным именем
            _options = new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new TestUsersDbContext(_options);
            _context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            // Удаляем базу данных после каждого теста
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public void AddUser_ShouldSaveToDatabase()
        {
            // Arrange: создаем нового пользователя
            var user = new UserModel
            {
                UserId = Guid.NewGuid(),
                Email = "test@gmail.com",
                PasswordHash = "hashedPassword",
                Role = "Author"
            };

            // Act: добавляем пользователя в базу данных
            _context.Users.Add(user);
            _context.SaveChanges();

            // Assert: проверяем, что пользователь сохранен
            var savedUser = _context.Users.FirstOrDefault(u => u.Email == "test@gmail.com");
            Assert.That(savedUser, Is.Not.Null, "User должен быть сохранен");
            Assert.That(savedUser.Email, Is.EqualTo("test@gmail.com"), "Email должен совпадать");
        }

        [Test]
        public void GetUserByEmail_ShouldReturnCorrectUser()
        {
            // Arrange: добавляем тестового пользователя
            var user = new UserModel
            {
                UserId = Guid.NewGuid(),
                Email = "test@gmail.com",
                PasswordHash = "hashedPassword",
                Role = "Author"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Act: получаем пользователя по email
            var fetchedUser = _context.Users.FirstOrDefault(u => u.Email == "test@gmail.com");
            
            Assert.That(fetchedUser, Is.Not.Null, "User не должен быть null");
            Assert.That(fetchedUser.UserId, Is.EqualTo(user.UserId), "User ID должен совпадать");
        }

        [Test]
        public void DeleteUser_ShouldRemoveFromDatabase()
        {
            // Arrange: добавляем тестового пользователя
            var user = new UserModel
            {
                UserId = Guid.NewGuid(),
                Email = "delete@gmail.com",
                PasswordHash = "hashedPassword",
                Role = "Author"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Act: удаляем пользователя
            _context.Users.Remove(user);
            _context.SaveChanges();

            // Assert: проверяем, что пользователь удален
            var deletedUser = _context.Users.FirstOrDefault(u => u.Email == "delete@gmail.com");
            Assert.That(deletedUser, Is.Null, "User должен быть удален");
        }
    }
}
