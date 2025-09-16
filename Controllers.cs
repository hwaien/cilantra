using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cilantra.Data;
using OtpNet;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TotpController : ControllerBase
{
    private readonly CilantraDbContext _context;

    public TotpController(CilantraDbContext context) => _context = context;

    // Register a new totp device
    [HttpGet("{username}/{deviceName}/key")]
    public async Task<ActionResult<string>> GetAuthenticatorKey(string username, string deviceName)
    {
        var user = await _context.Users.Include(u => u.TotpDevices).FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            user = new User { Username = username };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
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
            _context.TotpDevices.Add(device);
            await _context.SaveChangesAsync();
        }

        return device.Secret;
    }


    // Verify a user-provided TOTP
    [HttpPost("{username}/{deviceName}/verify")]
    public async Task<ActionResult> Verify(string username, string deviceName, [FromQuery] string code)
    {
        var user = await _context.Users.Include(u => u.TotpDevices).FirstOrDefaultAsync(u => u.Username == username);

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
        bool isValid = totp.VerifyTotp(code, out long _);

        if (isValid)
        {
            device.Verified = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        return Unauthorized();
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromQuery] string username, [FromQuery] string code)
    {
        var user = await _context.Users
            .Include(u => u.TotpDevices)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null) return Unauthorized();

        foreach (var device in user.TotpDevices.Where(d => d.Verified))
        {
            var totp = new Totp(Base32Encoding.ToBytes(device.Secret));
            bool isValid = totp.VerifyTotp(code, out long _, VerificationWindow.RfcSpecifiedNetworkDelay);
            if (isValid)
                return Ok();
        }

        return Unauthorized();
    }
}
