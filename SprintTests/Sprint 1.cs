using Xunit;
using Assign_2;

namespace SprintUnitTests;

public class UserDatabaseTests
{
    [Fact]
    public void Login_ValidAdminCredentials_ReturnsTrueAndAdminRole()
    {
        bool success = UserDatabase.Login("admin", "admin123", out string role);
        Assert.True(success);
        Assert.Equal("Admin", role);
    }

    [Fact]
    public void Login_ValidUserCredentials_ReturnsTrueAndUserRole()
    {
        bool success = UserDatabase.Login("alex", "user123", out string role);
        Assert.True(success);
        Assert.Equal("User", role);
    }

    [Fact]
    public void Login_InvalidPassword_ReturnsFalse()
    {
        bool success = UserDatabase.Login("admin", "wrongpassword", out string role);
        Assert.False(success);
    }

    [Fact]
    public void Login_NonExistentUser_ReturnsFalse()
    {
        bool success = UserDatabase.Login("nonexistentuser", "password", out string role);
        Assert.False(success);
    }

    [Fact]
    public void Register_NewUser_SuccessfullyAddsUser()
    {
        string uniqueUser = "testuser_" + Guid.NewGuid().ToString().Substring(0, 6);
        bool registered = UserDatabase.Register(uniqueUser, "pass123", "User");
        bool loginSuccess = UserDatabase.Login(uniqueUser, "pass123", out string role);

        Assert.True(registered);
        Assert.True(loginSuccess);
        Assert.Equal("User", role);
    }

    [Fact]
    public void Register_DuplicateUsername_ThrowsException()
    {
        string duplicateUser = "admin";

        Assert.Throws<System.Exception>(() =>
        {
            UserDatabase.Register(duplicateUser, "somepassword", "User");
        });
    }
}