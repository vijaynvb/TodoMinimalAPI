using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoMinimalAPI.Model;
using TodoMinimalAPI.Models;

namespace TodoMinimalAPI.Data
{
    public class TodoDBContext : IdentityDbContext<ApplicationUser>
    {
        public TodoDBContext()
        {

        }
        public TodoDBContext(DbContextOptions<TodoDBContext> options):base(options)
        {
        }
        public DbSet<Todo> Todos => Set<Todo>();
    }
}
