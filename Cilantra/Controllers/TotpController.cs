using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cilantra.Data;
using OtpNet;

namespace Cilantra.Controllers;

[ApiController]
[Route("api/[controller]")]
#pragma warning disable CA1812, CA1515
public sealed partial class TotpController(CilantraDbContext context, ILogger<TotpController> logger) : ControllerBase
#pragma warning restore CA1812, CA1515
{
    private readonly CilantraDbContext _context = context;
    private readonly ILogger<TotpController> _logger = logger;

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
            LogUserCreation(username);
            user = new User { Username = username };
            this._context.Users.Add(user);
            await this._context.SaveChangesAsync().ConfigureAwait(true);
        }

        var device = user.TotpDevices.FirstOrDefault(d => d.Name == deviceName);

        if (device is null)
        {
            LogDeviceCreation(deviceName);
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
            LogUnknownUserVerification(username);
            return NotFound();
        }

        var device = user.TotpDevices.FirstOrDefault(d => d.Name == deviceName);

        if (device is null)
        {
            LogUnknownDeviceVerification(deviceName, username);
            return NotFound();
        }

        var secretBytes = Base32Encoding.ToBytes(device.Secret);
        var totp = new Totp(secretBytes);
        var isValid = totp.VerifyTotp(code, out var _);

        if (isValid)
        {
            device.Verified = true;
            await this._context.SaveChangesAsync().ConfigureAwait(true);
            LogSuccessfulVerification(deviceName, username);
            return Ok();
        }

        LogInvalidCode(deviceName, username);
        return Unauthorized();
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromQuery] string username, [FromQuery] string code)
    {
        var user = await this._context.Users
            .Include(u => u.TotpDevices)
            .FirstOrDefaultAsync(u => u.Username == username)
            .ConfigureAwait(true);

        if (user is null)
        {
            LogUnknownUserLogin(username);
            return Unauthorized();
        }

        foreach (var device in user.TotpDevices.Where(d => d.Verified))
        {
            var totp = new Totp(Base32Encoding.ToBytes(device.Secret));
            var isValid = totp.VerifyTotp(code, out var _, VerificationWindow.RfcSpecifiedNetworkDelay);
            if (isValid)
            {
                LogSuccessfulLogin(username);
                return Ok();
            }
        }

        LogFailedLogin(username);
        return Unauthorized();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating new user {username}...")]
    private partial void LogUserCreation(string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating new device {deviceName}...")]
    private partial void LogDeviceCreation(string deviceName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Detected attempt to verify device for unknown user {username}")]
    private partial void LogUnknownUserVerification(string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Detected attempt to verify unknown device {deviceName} for user {username}")]
    private partial void LogUnknownDeviceVerification(string deviceName, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Detected attempt to use invalid code to verify device {deviceName} for user {username}")]
    private partial void LogInvalidCode(string deviceName, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Verified device {deviceName} for user {username}")]
    private partial void LogSuccessfulVerification(string deviceName, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Detected attempt to log in unknown user {username}")]
    private partial void LogUnknownUserLogin(string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {username} successfully logged in")]
    private partial void LogSuccessfulLogin(string username);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Detected failed login for user {username}")]
    private partial void LogFailedLogin(string username);

}
