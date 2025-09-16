namespace Cilantra.Data;

#pragma warning disable CA1515
public sealed class TotpDevice
#pragma warning restore CA1515
{
    public int Id { get; set; } // Primary key
    public string Name { get; set; } = "";
    public required string Secret { get; set; }
    public bool Verified { get; set; }
    public required User User { get; set; } // navigation property
    public int UserId { get; set; } // foreign key
}
