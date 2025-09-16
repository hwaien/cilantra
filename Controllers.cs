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

    // Register a new user with secret key
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(string username)
    {
        // Generate random 20-byte secret
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        var user = new User { Username = username, Secret = base32Secret };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    // Get current TOTP code for a user
    [HttpGet("{username}/current")]
    public async Task<ActionResult<string>> GetCurrentCode(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return NotFound();

        var secretBytes = Base32Encoding.ToBytes(user.Secret);
        var totp = new Totp(secretBytes);
        var code = totp.ComputeTotp();

        return code;
    }

    // Verify a user-provided TOTP
    [HttpPost("{username}/verify")]
    public async Task<ActionResult> Verify(string username, [FromQuery] string code)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return NotFound();

        var secretBytes = Base32Encoding.ToBytes(user.Secret);
        var totp = new Totp(secretBytes);

        bool isValid = totp.VerifyTotp(code, out long _);

        return isValid ? Ok() : Unauthorized();
    }
}
