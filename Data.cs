using Microsoft.EntityFrameworkCore;

namespace Cilantra.Data;

public class User
{
    public int Id { get; set; }           // Primary key
    public string Username { get; set; } = "";
    public string Secret { get; set; } = "";
}

public class CilantraDbContext : DbContext
{
    public CilantraDbContext(DbContextOptions<CilantraDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}
