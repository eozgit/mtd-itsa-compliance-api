using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using api.Models;
using api.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using Xunit.Abstractions;
// Removed: using System.Diagnostics;
// Removed: using static System.Console;

namespace api.Tests.Integration;

public class BusinessAndQuarterIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public BusinessAndQuarterIntegrationTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    private async Task<AuthResponse> RegisterAndLoginUser(string email, string username, string password)
    {
        var registerRequest = new RegisterRequest { Email = email, UserName = username, Password = password };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = JsonSerializer.Deserialize<AuthResponse>(await loginResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return authResponse!;
    }

    [Fact]
    public async Task BusinessRegistration_InitializesQuartersAndReturnsBusinessDetails()
    {
        _factory.ResetDatabase();

        // Arrange
        var email = $"business_reg_{Guid.NewGuid()}@example.com";
        var username = "BizUser";
        var password = "BizPassword123!";
        var businessName = "Test Business Inc.";
        var startDate = new DateTime(2025, 4, 6, 0, 0, 0, DateTimeKind.Utc); // MTD fiscal year start

        var authResponse = await RegisterAndLoginUser(email, username, password);
        _output.WriteLine($"AuthResponse UserId: {authResponse.UserId}");

        var capturedQuarterlyUpdates = new List<QuarterlyUpdate>();
        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.InsertManyAsync(
                It.IsAny<IEnumerable<QuarterlyUpdate>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<QuarterlyUpdate>, InsertManyOptions, CancellationToken>(
                (docs, opt, token) => capturedQuarterlyUpdates.AddRange(docs))
            .Returns(Task.CompletedTask);

        // Act
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var businessRequest = new BusinessRequest { Name = businessName, StartDate = startDate };
        var content = new StringContent(JsonSerializer.Serialize(businessRequest), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/business", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var businessResponse = JsonSerializer.Deserialize<BusinessResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(businessResponse);
        Assert.True(businessResponse.BusinessId > 0);
        Assert.Equal(businessName, businessResponse.Name);

        _output.WriteLine($"BusinessResponse BusinessId: {businessResponse.BusinessId}");

        // Verify business was added to the in-memory SQL database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var business = await dbContext.Businesses.FirstOrDefaultAsync(b => b.Name == businessName);
            Assert.NotNull(business);
            Assert.Equal(authResponse.UserId, business.UserId);
            Assert.Equal(businessName, business.Name);
            Assert.Equal(startDate, business.StartDate);
        }

        // Verify that 4 quarterly updates were inserted into the mock MongoDB collection
        _factory.MockQuarterlyUpdatesCollection.Verify(
            c => c.InsertManyAsync(
                It.Is<IEnumerable<QuarterlyUpdate>>(docs => docs.Count() == 4),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine($"--- Debugging Counts Before Quarters Loop ---");
        _output.WriteLine($"Captured Quarterly Updates Count: {capturedQuarterlyUpdates.Count}");
        _output.WriteLine($"Business Response BusinessId: {businessResponse.BusinessId}");
        _output.WriteLine($"Auth Response UserId: {authResponse.UserId}");
        _output.WriteLine($"-------------------------------------------");


        Assert.Equal(4, capturedQuarterlyUpdates.Count);
        foreach (var quarter in capturedQuarterlyUpdates)
        {
            Assert.NotNull(quarter.Id);

            // Reverted to Assert.Equal for int comparison, as the parsing logic is now fixed.
            _output.WriteLine($"Attempting BusinessId assertion: Expected {businessResponse.BusinessId}, Actual {quarter.BusinessId}");
            Assert.Equal(businessResponse.BusinessId, quarter.BusinessId);

            Assert.Equal("DRAFT", quarter.Status);
            Assert.Equal(0.00m, quarter.TaxableIncome);
            Assert.Equal(0.00m, quarter.AllowableExpenses);
            Assert.Equal(0.00m, quarter.NetProfit);
            Assert.NotNull(quarter.TaxYear);
            Assert.NotNull(quarter.QuarterName);
        }
    }
}
