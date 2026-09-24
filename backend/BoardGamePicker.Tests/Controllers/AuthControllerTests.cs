using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BoardGamePicker.API.Controllers;
using BoardGamePicker.API.DTOs;
using BoardGamePicker.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoardGamePicker.Tests.Controllers;

public class AuthControllerTests
{
    private const string TestSecret = "test-secret-long-enough-for-hmac-256!!";

    private static AuthController Controller() => ControllerWith(DbContextFactory.Create());

    private static AuthController ControllerWith(BoardGamePicker.API.Data.AppDbContext ctx) =>
        new(ctx, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = TestSecret })
            .Build());

    // Decode the JWT and return its claims without validating the signature,
    // so tests can inspect payload structure without wiring up full auth middleware.
    private static IEnumerable<Claim> ReadClaims(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;

    // ── Register — happy paths ────────────────────────────────────────────────

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
    public async Task Register_EmailStoredAsLowercase()
    {
        var result = await Controller().Register(new RegisterDto("Upper@Example.COM", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("upper@example.com", dto.Email);
    }

    [Fact]
    public async Task Register_DefaultRoleIsUser()
    {
        var result = await Controller().Register(new RegisterDto("new@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("user", Assert.IsType<AuthResponseDto>(ok.Value).Role);
    }

    [Fact]
    public async Task Register_TokenContainsCorrectEmailClaim()
    {
        var result = await Controller().Register(new RegisterDto("claims@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.IsType<AuthResponseDto>(ok.Value).Token;
        var claims = ReadClaims(token);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == "claims@example.com");
    }

    [Fact]
    public async Task Register_TokenContainsUserRoleClaim()
    {
        var result = await Controller().Register(new RegisterDto("role@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.IsType<AuthResponseDto>(ok.Value).Token;
        var claims = ReadClaims(token);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
    }

    [Fact]
    public async Task Register_TokenContainsNameIdentifierClaim()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        var result = await controller.Register(new RegisterDto("id@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.IsType<AuthResponseDto>(ok.Value).Token;
        var claims = ReadClaims(token);
        // NameIdentifier must be a positive integer matching the stored user id
        var idClaim = Assert.Single(claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.True(int.TryParse(idClaim.Value, out var id) && id > 0);
    }

    // ── Register — sad paths ─────────────────────────────────────────────────

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("dupe@example.com", "password123"));
        var result = await controller.Register(new RegisterDto("dupe@example.com", "different"));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_DuplicateEmailCaseInsensitive_Returns409()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("user@example.com", "password123"));
        var result = await controller.Register(new RegisterDto("USER@EXAMPLE.COM", "different"));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ── Login — happy paths ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_CorrectCredentials_Returns200WithToken()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("login@example.com", "mypassword"));
        var result = await controller.Login(new LoginDto("login@example.com", "mypassword"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(dto.Token));
        Assert.Equal("login@example.com", dto.Email);
    }

    [Fact]
    public async Task Login_EmailCaseInsensitive_Succeeds()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("mixed@example.com", "mypassword"));
        var result = await controller.Login(new LoginDto("MIXED@EXAMPLE.COM", "mypassword"));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_TokenContainsCorrectRoleClaim()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("role@example.com", "password123"));
        var result = await controller.Login(new LoginDto("role@example.com", "password123"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.IsType<AuthResponseDto>(ok.Value).Token;
        Assert.Contains(ReadClaims(token), c => c.Type == ClaimTypes.Role && c.Value == "user");
    }

    // ── Login — sad paths ────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

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

    [Fact]
    public async Task Login_CorrectEmailWrongCase_WrongPassword_Returns401()
    {
        var ctx = DbContextFactory.Create();
        var controller = ControllerWith(ctx);

        await controller.Register(new RegisterDto("user@example.com", "correct"));
        var result = await controller.Login(new LoginDto("USER@EXAMPLE.COM", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}
