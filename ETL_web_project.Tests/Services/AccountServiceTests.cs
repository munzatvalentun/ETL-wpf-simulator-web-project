using AutoMapper;
using ETL_web_project.Data.Context;
using Xunit;
using ETL_web_project.Data.Entities;
using ETL_web_project.DTOs.Account;
using ETL_web_project.Enums;
using ETL_web_project.Handlers;
using ETL_web_project.Mappings;
using ETL_web_project.Services;
using Microsoft.EntityFrameworkCore;

namespace ETL_web_project.Tests.Services;

public class AccountServiceTests
{
    private static ProjectContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ProjectContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ProjectContext(options);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserAccountProfile>());
        return config.CreateMapper();
    }

    private static UserAccount ActiveUser(string username = "testuser", string password = "Password1!")
        => new()
        {
            Username     = username,
            Email        = $"{username}@example.com",
            PasswordHash = PasswordHashHandler.HashPassword(password),
            IsActive     = true,
            Role         = UserRole.Analyst,
            CreatedAt    = DateTime.Now
        };

    // --- ValidateUserAsync ---

    [Fact]
    public async Task ValidateUserAsync_ValidCredentials_ReturnsUserDto()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_ValidCredentials_ReturnsUserDto));
        db.UserAccounts.Add(ActiveUser("alice", "Secret123!"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ValidateUserAsync(new LoginDto { Username = "alice", Password = "Secret123!" });

        Assert.NotNull(result);
        Assert.Equal("alice", result.Username);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task ValidateUserAsync_WrongPassword_ReturnsNull()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_WrongPassword_ReturnsNull));
        db.UserAccounts.Add(ActiveUser("bob", "CorrectPass1!"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ValidateUserAsync(new LoginDto { Username = "bob", Password = "WrongPass1!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUserAsync_UnknownUser_ReturnsNull()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_UnknownUser_ReturnsNull));
        var service = new AccountService(db, CreateMapper());

        var result = await service.ValidateUserAsync(new LoginDto { Username = "nobody", Password = "anything" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUserAsync_InactiveUser_ReturnsNull()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_InactiveUser_ReturnsNull));
        var user = ActiveUser("carol", "Pass1234!");
        user.IsActive = false;
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ValidateUserAsync(new LoginDto { Username = "carol", Password = "Pass1234!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUserAsync_ValidLogin_UpdatesLastLoginAt()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_ValidLogin_UpdatesLastLoginAt));
        db.UserAccounts.Add(ActiveUser("dave", "Pass1234!"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());
        var before  = DateTime.Now.AddSeconds(-1);

        await service.ValidateUserAsync(new LoginDto { Username = "dave", Password = "Pass1234!" });

        var user = await db.UserAccounts.FirstAsync(u => u.Username == "dave");
        Assert.NotNull(user.LastLoginAt);
        Assert.True(user.LastLoginAt >= before);
    }

    [Fact]
    public async Task ValidateUserAsync_ValidCredentials_ReturnsCorrectRole()
    {
        using var db = CreateContext(nameof(ValidateUserAsync_ValidCredentials_ReturnsCorrectRole));
        var user = ActiveUser("engineer", "Pass1234!");
        user.Role = UserRole.DataEngineer;
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ValidateUserAsync(new LoginDto { Username = "engineer", Password = "Pass1234!" });

        Assert.Equal(UserRole.DataEngineer, result!.Role);
    }

    // --- UsernameExistsAsync ---

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        using var db = CreateContext(nameof(UsernameExistsAsync_ExistingUsername_ReturnsTrue));
        db.UserAccounts.Add(ActiveUser("eve"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        Assert.True(await service.UsernameExistsAsync("eve"));
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        using var db = CreateContext(nameof(UsernameExistsAsync_NonExistingUsername_ReturnsFalse));
        var service = new AccountService(db, CreateMapper());

        Assert.False(await service.UsernameExistsAsync("ghost"));
    }

    // --- EmailExistsAsync ---

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        using var db = CreateContext(nameof(EmailExistsAsync_ExistingEmail_ReturnsTrue));
        db.UserAccounts.Add(ActiveUser("frank"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        Assert.True(await service.EmailExistsAsync("frank@example.com"));
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        using var db = CreateContext(nameof(EmailExistsAsync_NonExistingEmail_ReturnsFalse));
        var service = new AccountService(db, CreateMapper());

        Assert.False(await service.EmailExistsAsync("nobody@example.com"));
    }

    // --- RegisterUserAsync ---

    [Fact]
    public async Task RegisterUserAsync_CreatesUserWithAnalystRole()
    {
        using var db = CreateContext(nameof(RegisterUserAsync_CreatesUserWithAnalystRole));
        var service = new AccountService(db, CreateMapper());
        var dto = new RegisterDto
        {
            FullName        = "Grace Hopper",
            Username        = "grace",
            Email           = "grace@example.com",
            Password        = "SecurePass1!",
            ConfirmPassword = "SecurePass1!"
        };

        var result = await service.RegisterUserAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("grace", result.Username);
        Assert.Equal(UserRole.Analyst, result.Role);
    }

    [Fact]
    public async Task RegisterUserAsync_UserIsActiveByDefault()
    {
        using var db = CreateContext(nameof(RegisterUserAsync_UserIsActiveByDefault));
        var service = new AccountService(db, CreateMapper());
        var dto = new RegisterDto
        {
            FullName        = "Henry Ford",
            Username        = "henry",
            Email           = "henry@example.com",
            Password        = "PlainPass1!",
            ConfirmPassword = "PlainPass1!"
        };

        var result = await service.RegisterUserAsync(dto);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task RegisterUserAsync_PasswordIsHashedNotStoredAsPlainText()
    {
        using var db = CreateContext(nameof(RegisterUserAsync_PasswordIsHashedNotStoredAsPlainText));
        var service = new AccountService(db, CreateMapper());
        var dto = new RegisterDto
        {
            FullName        = "Ida Lovelace",
            Username        = "ida",
            Email           = "ida@example.com",
            Password        = "PlainPass1!",
            ConfirmPassword = "PlainPass1!"
        };

        await service.RegisterUserAsync(dto);

        var user = await db.UserAccounts.FirstAsync(u => u.Username == "ida");
        Assert.NotEqual("PlainPass1!", user.PasswordHash);
        Assert.True(PasswordHashHandler.VerifyPassword("PlainPass1!", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterUserAsync_PersistsUserToDatabase()
    {
        using var db = CreateContext(nameof(RegisterUserAsync_PersistsUserToDatabase));
        var service = new AccountService(db, CreateMapper());
        var dto = new RegisterDto
        {
            FullName        = "Jack Kilby",
            Username        = "jack",
            Email           = "jack@example.com",
            Password        = "SecurePass1!",
            ConfirmPassword = "SecurePass1!"
        };

        await service.RegisterUserAsync(dto);

        Assert.Equal(1, await db.UserAccounts.CountAsync());
    }

    // --- GeneratePasswordResetTokenAsync ---

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_ActiveUserByEmail_ReturnsToken()
    {
        using var db = CreateContext(nameof(GeneratePasswordResetTokenAsync_ActiveUserByEmail_ReturnsToken));
        db.UserAccounts.Add(ActiveUser("ivan"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var token = await service.GeneratePasswordResetTokenAsync("ivan@example.com");

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_TokenIsSavedToUser()
    {
        using var db = CreateContext(nameof(GeneratePasswordResetTokenAsync_TokenIsSavedToUser));
        db.UserAccounts.Add(ActiveUser("julia"));
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var token = await service.GeneratePasswordResetTokenAsync("julia@example.com");

        var user = await db.UserAccounts.FirstAsync(u => u.Username == "julia");
        Assert.Equal(token, user.ResetToken);
        Assert.NotNull(user.ResetTokenExpires);
        Assert.True(user.ResetTokenExpires > DateTime.Now);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_NonExistingEmail_ReturnsNull()
    {
        using var db = CreateContext(nameof(GeneratePasswordResetTokenAsync_NonExistingEmail_ReturnsNull));
        var service = new AccountService(db, CreateMapper());

        var token = await service.GeneratePasswordResetTokenAsync("ghost@example.com");

        Assert.Null(token);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_InactiveUser_ReturnsNull()
    {
        using var db = CreateContext(nameof(GeneratePasswordResetTokenAsync_InactiveUser_ReturnsNull));
        var user = ActiveUser("kim");
        user.IsActive = false;
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var token = await service.GeneratePasswordResetTokenAsync("kim@example.com");

        Assert.Null(token);
    }

    // --- ResetPasswordAsync ---

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ChangesPasswordAndReturnsTrue()
    {
        using var db = CreateContext(nameof(ResetPasswordAsync_ValidToken_ChangesPasswordAndReturnsTrue));
        var token = Guid.NewGuid().ToString("N");
        var user  = ActiveUser("leo", "OldPass1!");
        user.ResetToken        = token;
        user.ResetTokenExpires = DateTime.Now.AddHours(1);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ResetPasswordAsync(token, "NewPass123!");

        Assert.True(result);
        var updated = await db.UserAccounts.FirstAsync(u => u.Username == "leo");
        Assert.True(PasswordHashHandler.VerifyPassword("NewPass123!", updated.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ClearsResetToken()
    {
        using var db = CreateContext(nameof(ResetPasswordAsync_ValidToken_ClearsResetToken));
        var token = Guid.NewGuid().ToString("N");
        var user  = ActiveUser("mia");
        user.ResetToken        = token;
        user.ResetTokenExpires = DateTime.Now.AddHours(1);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        await service.ResetPasswordAsync(token, "NewPass123!");

        var updated = await db.UserAccounts.FirstAsync(u => u.Username == "mia");
        Assert.Null(updated.ResetToken);
        Assert.Null(updated.ResetTokenExpires);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalse()
    {
        using var db = CreateContext(nameof(ResetPasswordAsync_ExpiredToken_ReturnsFalse));
        var token = Guid.NewGuid().ToString("N");
        var user  = ActiveUser("nina");
        user.ResetToken        = token;
        user.ResetTokenExpires = DateTime.Now.AddHours(-1);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        var service = new AccountService(db, CreateMapper());

        var result = await service.ResetPasswordAsync(token, "NewPass123!");

        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsFalse()
    {
        using var db = CreateContext(nameof(ResetPasswordAsync_InvalidToken_ReturnsFalse));
        var service = new AccountService(db, CreateMapper());

        var result = await service.ResetPasswordAsync("completely-invalid-token", "NewPass123!");

        Assert.False(result);
    }
}
