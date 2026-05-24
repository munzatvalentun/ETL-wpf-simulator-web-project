using ETL_web_project.Handlers;
using Xunit;

namespace ETL_web_project.Tests.Handlers;

public class PasswordHashHandlerTests
{
    // --- HashPassword ---

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var hash = PasswordHashHandler.HashPassword("Password123!");
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ReturnsValidBase64()
    {
        var hash  = PasswordHashHandler.HashPassword("Password123!");
        var bytes = Convert.FromBase64String(hash);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void HashPassword_SamePassword_ProducesDifferentHashes()
    {
        var h1 = PasswordHashHandler.HashPassword("SamePassword1!");
        var h2 = PasswordHashHandler.HashPassword("SamePassword1!");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
    {
        var h1 = PasswordHashHandler.HashPassword("PasswordA1!");
        var h2 = PasswordHashHandler.HashPassword("PasswordB2!");
        Assert.NotEqual(h1, h2);
    }

    // --- VerifyPassword: correct credentials ---

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        const string password = "MySecurePass1!";
        var hash = PasswordHashHandler.HashPassword(password);

        Assert.True(PasswordHashHandler.VerifyPassword(password, hash));
    }

    [Theory]
    [InlineData("Password1!")]
    [InlineData("AnotherPass!2")]
    [InlineData("12345678")]
    [InlineData("!!@@##$$%%")]
    public void VerifyPassword_VariousPasswords_RoundTripSucceeds(string password)
    {
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.True(PasswordHashHandler.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_SpecialCharacters_RoundTrip()
    {
        const string password = "P@$$w0rd!#%&*()[]{}";
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.True(PasswordHashHandler.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_UnicodePassword_RoundTrip()
    {
        const string password = "Пароль123!";
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.True(PasswordHashHandler.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_VeryLongPassword_RoundTrip()
    {
        var password = new string('a', 500) + "1!";
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.True(PasswordHashHandler.VerifyPassword(password, hash));
    }

    // --- VerifyPassword: wrong credentials ---

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHashHandler.HashPassword("CorrectPassword1!");
        Assert.False(PasswordHashHandler.VerifyPassword("WrongPassword1!", hash));
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        var hash = PasswordHashHandler.HashPassword("SomePassword1!");
        Assert.False(PasswordHashHandler.VerifyPassword("", hash));
    }

    [Fact]
    public void VerifyPassword_PasswordWithExtraSpace_ReturnsFalse()
    {
        const string password = "Password1!";
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.False(PasswordHashHandler.VerifyPassword(password + " ", hash));
    }

    [Fact]
    public void VerifyPassword_CaseDifference_ReturnsFalse()
    {
        const string password = "Password1!";
        var hash = PasswordHashHandler.HashPassword(password);
        Assert.False(PasswordHashHandler.VerifyPassword("password1!", hash));
    }

    // --- VerifyPassword: malformed hash ---

    [Fact]
    public void VerifyPassword_InvalidBase64Hash_ReturnsFalse()
    {
        Assert.False(PasswordHashHandler.VerifyPassword("password", "not-valid-base64!!!"));
    }

    [Fact]
    public void VerifyPassword_TooShortHash_ReturnsFalse()
    {
        var shortHash = Convert.ToBase64String(new byte[5]);
        Assert.False(PasswordHashHandler.VerifyPassword("password", shortHash));
    }

    [Fact]
    public void VerifyPassword_EmptyHash_ReturnsFalse()
    {
        Assert.False(PasswordHashHandler.VerifyPassword("password", ""));
    }

    [Fact]
    public void VerifyPassword_HashOfDifferentPassword_ReturnsFalse()
    {
        var hash = PasswordHashHandler.HashPassword("PasswordA1!");
        Assert.False(PasswordHashHandler.VerifyPassword("PasswordB1!", hash));
    }

    // --- Salt uniqueness ---

    [Fact]
    public void HashPassword_EachHashHasUniqueSalt()
    {
        var hashes = Enumerable.Range(0, 10)
            .Select(_ => PasswordHashHandler.HashPassword("TestPassword1!"))
            .ToList();

        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }
}
