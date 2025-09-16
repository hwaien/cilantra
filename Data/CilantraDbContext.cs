using Microsoft.EntityFrameworkCore;

namespace Cilantra.Data;

#pragma warning disable CA1812, CA1515
public sealed class CilantraDbContext(DbContextOptions<CilantraDbContext> options) : DbContext(options)
#pragma warning restore CA1812, CA1515
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TotpDevice> TotpDevices => Set<TotpDevice>();
}
