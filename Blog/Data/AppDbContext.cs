using Blog.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Blog.Data
{
    public class UsersDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<UserModel> Users { get; set; }

        public UsersDbContext(DbContextOptions<UsersDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("UsersDbConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    public class PostsDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<PostModel> Posts { get; set; }
        
        public PostsDbContext(DbContextOptions<PostsDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _configuration.GetConnectionString("PostsDbConnection");
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}