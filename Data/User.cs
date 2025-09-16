namespace Cilantra.Data;

#pragma warning disable CA1515
public sealed class User
#pragma warning restore CA1515
{
    public int Id { get; set; } // Primary key

    public string Username { get; set; } = "";

#pragma warning disable CA2227
    public ICollection<TotpDevice> TotpDevices { get; set; } = []; // collection navigation
#pragma warning restore CA2227
}
