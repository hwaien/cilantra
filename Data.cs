using Microsoft.EntityFrameworkCore;

namespace Cilantra.Data;


public class TotpDevice
{
    public int Id { get; set; } // Primary key
    public string Name { get; set; } = "";
    public required string Secret { get; set; }
    public bool Verified { get; set; } = false;
    public required User User { get; set; } // navigation property
    public int UserId { get; set; } // foreign key
}

public class User
{
    public int Id { get; set; } // Primary key
    public string Username { get; set; } = "";
    public ICollection<TotpDevice> TotpDevices { get; set; } = []; // collection navigation
}

public class CilantraDbContext : DbContext
{
    public CilantraDbContext(DbContextOptions<CilantraDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TotpDevice> TotpDevices => Set<TotpDevice>();
}
