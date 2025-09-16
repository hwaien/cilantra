using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

using Cilantra.Data;
using Cilantra.Controllers;

namespace Cilantra.Tests;

public class TotpControllerTests
{
    private CilantraDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CilantraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CilantraDbContext(options);
    }

    [Fact]
    public async Task GetAuthenticatorKey_CreatesUser_IfUserIsNew()
    {
        // Arrange
        var newUsername = "NEWUSER";
        var context = GetInMemoryDbContext();
        var loggerMock = new Mock<ILogger<TotpController>>();
        var sut = new TotpController(context, loggerMock.Object);
        var userExists = await context.Users.AnyAsync(u => u.Username == newUsername);
        Assert.False(userExists);

        // Act
        await sut.GetAuthenticatorKey(newUsername, "device");

        // Assert
        userExists = await context.Users.AnyAsync(u => u.Username == newUsername);
        Assert.True(userExists);
    }
}
