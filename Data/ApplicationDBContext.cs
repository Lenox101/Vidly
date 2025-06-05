using Microsoft.EntityFrameworkCore;
using Vidly.Models;

namespace Vidly.Data
{
    public class VidlyContext : DbContext
    {
        public VidlyContext(DbContextOptions<VidlyContext> options)
            : base(options)
        {

        }

        public DbSet<User> Users { get; set; } // Replace with your actual model(s)
        public DbSet<Todo> Todos { get; set; }
    }
}
