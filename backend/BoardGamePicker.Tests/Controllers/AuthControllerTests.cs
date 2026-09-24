using BoardGamePicker.API.Controllers;
using BoardGamePicker.API.DTOs;
using BoardGamePicker.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoardGamePicker.Tests.Controllers;

public class AuthControllerTests
{
    private static AuthController Controller() =>
        new(DbContextFactory.Create(), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SECRET"] = "test-secret-long-enough-for-hmac-256!!",
            })
            .Build());

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewEmail_Returns200WithToken()
    {
        var result = await Controller().Register(new RegisterDto("test@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("test@example.com", dto.Email);
        Assert.Equal("user", dto.Role);
        Assert.False(string.IsNullOrEmpty(dto.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var ctx = DbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = "test-secret-long-enough-for-hmac-256!!" })
            .Build();
        var controller = new AuthController(ctx, config);

        await controller.Register(new RegisterDto("dupe@example.com", "password123"));
        var result = await controller.Register(new RegisterDto("dupe@example.com", "different"));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_EmailStoredAsLowercase()
    {
        var ctx = DbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = "test-secret-long-enough-for-hmac-256!!" })
            .Build();
        var controller = new AuthController(ctx, config);

        var result = await controller.Register(new RegisterDto("Upper@Example.COM", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("upper@example.com", dto.Email);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithToken()
    {
        var ctx = DbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = "test-secret-long-enough-for-hmac-256!!" })
            .Build();
        var controller = new AuthController(ctx, config);

        await controller.Register(new RegisterDto("login@example.com", "mypassword"));
        var result = await controller.Login(new LoginDto("login@example.com", "mypassword"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(dto.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var ctx = DbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = "test-secret-long-enough-for-hmac-256!!" })
            .Build();
        var controller = new AuthController(ctx, config);

        await controller.Register(new RegisterDto("user@example.com", "correct"));
        var result = await controller.Login(new LoginDto("user@example.com", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var result = await Controller().Login(new LoginDto("nobody@example.com", "anything"));
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
