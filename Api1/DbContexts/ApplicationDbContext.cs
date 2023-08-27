using Microsoft.EntityFrameworkCore;

namespace Api1.DbContexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Database.EnsureCreated();
        }
        public DbSet<Shared.Models.Book> Books { get; set; } = null!;
    }
}
