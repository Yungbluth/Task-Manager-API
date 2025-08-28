using Microsoft.EntityFrameworkCore;

namespace TaskApi;

public class AppDb : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();

    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
}