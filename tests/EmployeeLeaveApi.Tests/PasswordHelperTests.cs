using EmployeeLeaveApi.Helpers;

namespace EmployeeLeaveApi.Tests;

public class PasswordHelperTests
{
    [Fact]
    public void HashPassword_ShouldReturnNonNullHash()
    {
        // Act
        var hash = PasswordHelper.HashPassword("TestPassword123");

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ShouldNotReturnPlaintext()
    {
        // Arrange
        var password = "MySecretPassword!@#";

        // Act
        var hash = PasswordHelper.HashPassword(password);

        // Assert
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_SamePlaintext_ShouldProduceDifferentHashes()
    {
        // BCrypt uses a random salt each time
        var password = "SamePassword";

        // Act
        var hash1 = PasswordHelper.HashPassword(password);
        var hash2 = PasswordHelper.HashPassword(password);

        // Assert - different hashes due to random salt
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "CorrectPassword123";
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "CorrectPassword123";
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword("WrongPassword", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_InvalidHash_ShouldReturnFalse()
    {
        // Act
        var result = PasswordHelper.VerifyPassword("password", "not-a-valid-bcrypt-hash");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyHash_ShouldReturnFalse()
    {
        // Act
        var result = PasswordHelper.VerifyPassword("password", "");

        // Assert
        Assert.False(result);
    }
}
