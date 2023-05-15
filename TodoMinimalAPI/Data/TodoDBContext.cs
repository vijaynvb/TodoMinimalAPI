using Microsoft.EntityFrameworkCore;
using TodoMinimalAPI.Model;

namespace TodoMinimalAPI.Data
{
    public class TodoDBContext : DbContext
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
