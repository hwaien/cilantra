using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cilantra.Data;
using OtpNet;

namespace Cilantra.Controllers;

[ApiController]
[Route("api/[controller]")]
#pragma warning disable CA1812, CA1515
public sealed class TotpController(CilantraDbContext context) : ControllerBase
#pragma warning restore CA1812, CA1515
{
    private readonly CilantraDbContext _context = context;

    // Register a new totp device
    [HttpGet("{username}/{deviceName}/key")]
    public async Task<ActionResult<string>> GetAuthenticatorKey(string username, string deviceName)
    {
        var user = await this._context.Users
            .Include(u => u.TotpDevices)
            .FirstOrDefaultAsync(u => u.Username == username)
            .ConfigureAwait(true);

        if (user is null)
        {
            user = new User { Username = username };
            this._context.Users.Add(user);
            await this._context.SaveChangesAsync().ConfigureAwait(true);
        }

        var device = user.TotpDevices.FirstOrDefault(d => d.Name == deviceName);

        if (device is null)
        {
            // Generate random 20-byte secret
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretKey);
            device = new TotpDevice
            {
                Name = deviceName,
                Secret = base32Secret,
                User = user,
            };
            this._context.TotpDevices.Add(device);
            await this._context.SaveChangesAsync().ConfigureAwait(true);
        }

        return device.Secret;
    }


    // Verify a user-provided TOTP
    [HttpPost("{username}/{deviceName}/verify")]
    public async Task<ActionResult> Verify(string username, string deviceName, [FromQuery] string code)
    {
        var user = await this._context.Users
            .Include(u => u.TotpDevices)
            .FirstOrDefaultAsync(u => u.Username == username)
            .ConfigureAwait(true);

        if (user is null)
        {
            return NotFound();
        }

        var device = user.TotpDevices.FirstOrDefault(d => d.Name == deviceName);

        if (device is null)
        {
            return NotFound();
        }

        var secretBytes = Base32Encoding.ToBytes(device.Secret);
        var totp = new Totp(secretBytes);
        var isValid = totp.VerifyTotp(code, out var _);

        if (isValid)
        {
            device.Verified = true;
            await this._context.SaveChangesAsync().ConfigureAwait(true);
            return Ok();
        }

        return Unauthorized();
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromQuery] string username, [FromQuery] string code)
    {
        var user = await this._context.Users
            .Include(u => u.TotpDevices)
            .FirstOrDefaultAsync(u => u.Username == username)
            .ConfigureAwait(true);

        if (user is null) return Unauthorized();

        foreach (var device in user.TotpDevices.Where(d => d.Verified))
        {
            var totp = new Totp(Base32Encoding.ToBytes(device.Secret));
            var isValid = totp.VerifyTotp(code, out var _, VerificationWindow.RfcSpecifiedNetworkDelay);
            if (isValid)
                return Ok();
        }

        return Unauthorized();
    }
}
