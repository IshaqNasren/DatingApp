using Microsoft.EntityFrameworkCore;
using TodoApi.Model;

namespace TodoApi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base (options) {}

        public DbSet<Value> Values { get; set;}
    }
}