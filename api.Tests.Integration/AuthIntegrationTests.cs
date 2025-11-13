using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using api.Models; // Assuming your DTOs (RegisterRequest, LoginRequest, AuthResponse) are in api.Models
using api.Data; // For ApplicationDbContext
using Microsoft.Extensions.DependencyInjection; // For CreateScope and GetRequiredService
using System.Linq;
using System;
using Microsoft.AspNetCore.Hosting; // Added for IWebHostBuilder (if needed by factory setup)
using Microsoft.EntityFrameworkCore; // Add this line

namespace api.Tests.Integration;

// IClassFixture ensures that the CustomWebApplicationFactory is initialized once for all tests in this class
public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // CreateClient ensures a new HttpClient is created for each test, configured with the factory
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Successful_User_Registration_Returns_Token_And_CreatesUser()
    {
        _factory.ResetDatabase(); // Reset the database for this test

        // Arrange
        var email = $"register_success_{Guid.NewGuid()}@example.com"; // Use unique email for each test
        var username = "TestUserSuccess";
        var password = "SecurePassword123!";

        var registerRequest = new RegisterRequest
        {
            Email = email,
            UserName = username,
            Password = password
        };
        var content = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.EnsureSuccessStatusCode(); // Checks for 2xx status code
        var responseString = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrEmpty(authResponse.Token));
        Assert.False(string.IsNullOrEmpty(authResponse.UserId));
        Assert.Equal(username, authResponse.UserName);

        // Verify user was added to the in-memory SQL database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            Assert.NotNull(user);
            Assert.Equal(registerRequest.Email, user.Email);
            Assert.Equal(registerRequest.UserName, user.UserName);
            Assert.False(string.IsNullOrEmpty(user.PasswordHash)); // Password hash should be set
        }
    }

    [Fact]
    public async Task Register_With_Existing_Email_Returns_Conflict()
    {
        _factory.ResetDatabase(); // Reset the database for this test

        // Arrange - Register a user first to create a conflict
        var email = $"register_duplicate_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            UserName = "DuplicateUser",
            Password = "SecurePassword123!"
        };
        var content = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", content); // First registration (should succeed)

        // Act - Try to register with the same email again
        var duplicateContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/register", duplicateContent);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var errorResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("User with this email already exists.", errorResponse);
    }

    [Fact]
    public async Task Successful_User_Login_Returns_Token()
    {
        _factory.ResetDatabase(); // Reset the database for this test

        // Arrange - Register a user first to have credentials to log in with
        var email = $"login_success_{Guid.NewGuid()}@example.com";
        var username = "LoginUser";
        var password = "LoginPassword123!";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            UserName = username,
            Password = password
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", registerContent); // Register the user

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.EnsureSuccessStatusCode(); // Checks for 2xx status code
        var responseString = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrEmpty(authResponse.Token));
        Assert.False(string.IsNullOrEmpty(authResponse.UserId));
        Assert.Equal(username, authResponse.UserName);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Returns_Unauthorized()
    {
        _factory.ResetDatabase(); // Reset the database for this test

        // Arrange - No registration means no valid user
        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent_{Guid.NewGuid()}@example.com",
            Password = "WrongPassword123!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
