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
using MongoDB.Bson; // NEW: Required for ObjectId
using System.Net.Http.Json; // NEW: Required for PostAsJsonAsync extension method
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



    // NEW TEST: GetQuarters_ReturnsQuartersWithFinancialSummaries
    [Fact]
    public async Task GetQuarters_ReturnsQuartersWithFinancialSummaries()
    {
        _factory.ResetDatabase();

        // Arrange
        var email = $"quarters_get_{Guid.NewGuid()}@example.com";
        var username = "QuartersUser";
        var password = "QuartersPassword123!";
        var businessName = "Quarters Business Ltd.";
        var startDate = new DateTime(2025, 4, 6, 0, 0, 0, DateTimeKind.Utc); // MTD fiscal year start

        var authResponse = await RegisterAndLoginUser(email, username, password);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var businessRequest = new BusinessRequest { Name = businessName, StartDate = startDate };
        var businessResponse = await _client.PostAsJsonAsync("/api/business", businessRequest);
        businessResponse.EnsureSuccessStatusCode();
        var businessContent = await businessResponse.Content.ReadAsStringAsync();
        var bizResponse = JsonSerializer.Deserialize<BusinessResponse>(businessContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(bizResponse);
        var businessId = bizResponse.BusinessId;

        // Manually create mock quarters that would be generated by the API logic
        // We need to simulate the state of MongoDB *after* business creation
        var mockQuarters = new List<QuarterlyUpdate>
        {
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q1", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q2", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q3", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q4", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m }
        };

        // Simulate some data enrichment for a quarter to test cumulative sums later
        var firstQuarterToUpdate = mockQuarters.First();
        firstQuarterToUpdate.TaxableIncome = 10000.00m;
        firstQuarterToUpdate.AllowableExpenses = 2000.00m;
        firstQuarterToUpdate.NetProfit = firstQuarterToUpdate.TaxableIncome - firstQuarterToUpdate.AllowableExpenses;

        // Mock MongoDB Find and ToListAsync for the /api/quarters endpoint
        // This is crucial: we mock what the MongoDB driver returns
        var mockAsyncCursor = new Mock<IAsyncCursor<QuarterlyUpdate>>();
        mockAsyncCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockAsyncCursor.SetupGet(_ => _.Current).Returns(mockQuarters);

        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAsyncCursor.Object);

        // Act
        var response = await _client.GetAsync("/api/quarters");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var quartersResponse = JsonSerializer.Deserialize<QuartersResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(quartersResponse);
        Assert.Equal(4, quartersResponse.Quarters.Count);

        // Verify the first quarter has updated profit
        var retrievedFirstQuarter = quartersResponse.Quarters.First();
        Assert.Equal(firstQuarterToUpdate.TaxableIncome, retrievedFirstQuarter.TaxableIncome);
        Assert.Equal(firstQuarterToUpdate.AllowableExpenses, retrievedFirstQuarter.AllowableExpenses);
        Assert.Equal(firstQuarterToUpdate.NetProfit, retrievedFirstQuarter.NetProfit); // NetProfit should be calculated in the API

        // Verify cumulative financial summaries
        // CORRECTED: Expected TotalNetProfitSubmitted should be 0.00m because no quarters are "SUBMITTED"
        Assert.Equal(0.00m, quartersResponse.TotalNetProfitSubmitted);
        Assert.Equal(0.00m, quartersResponse.CumulativeEstimatedTaxLiability);

        _factory.MockQuarterlyUpdatesCollection.Verify(
            c => c.FindAsync(
                It.Is<FilterDefinition<QuarterlyUpdate>>(filter => filter != null),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateQuarter_SavesDraftDataAndCalculatesNetProfit()
    {
        _factory.ResetDatabase();

        // Arrange
        var email = $"quarter_update_{Guid.NewGuid()}@example.com";
        var username = "UpdateUser";
        var password = "UpdatePassword123!";
        var businessName = "Update Business Co.";
        var startDate = new DateTime(2025, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        var authResponse = await RegisterAndLoginUser(email, username, password);

        // Register a business (this will trigger the creation of 4 DRAFT quarters in MongoDB)
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var businessRequest = new BusinessRequest { Name = businessName, StartDate = startDate };
        var businessResponse = await _client.PostAsJsonAsync("/api/business", businessRequest);
        businessResponse.EnsureSuccessStatusCode();
        var businessContent = await businessResponse.Content.ReadAsStringAsync();
        var bizResponse = JsonSerializer.Deserialize<BusinessResponse>(businessContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(bizResponse);
        var businessId = bizResponse.BusinessId;

        // Manually create mock quarters as they would exist in MongoDB initially (all DRAFT)
        var initialQuarters = new List<QuarterlyUpdate>
        {
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q1", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q2", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q3", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q4", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m }
        };

        // We need to capture the ID of a quarter to update. Let's pick the first one.
        var quarterToUpdateId = initialQuarters.First().Id;

        // Mock FindAsync to return the initial quarters when the API endpoint tries to retrieve them
        var mockAsyncCursorFind = new Mock<IAsyncCursor<QuarterlyUpdate>>();
        mockAsyncCursorFind.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockAsyncCursorFind.SetupGet(_ => _.Current).Returns(initialQuarters);

        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAsyncCursorFind.Object);


        // Capture the updated quarter document that ReplaceOneAsync is called with
        QuarterlyUpdate? capturedUpdate = null;
        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<QuarterlyUpdate>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<QuarterlyUpdate>, QuarterlyUpdate, ReplaceOptions, CancellationToken>(
                (filter, update, options, token) => capturedUpdate = update)
            .ReturnsAsync(new Mock<ReplaceOneResult>().Object); // Return a successful mock result

        // Act
        var updateRequest = new QuarterlyUpdateRequest
        {
            TaxableIncome = 15000.50m,
            AllowableExpenses = 3000.25m
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/quarter/{quarterToUpdateId}", content);


        // Assert
        response.EnsureSuccessStatusCode(); // Expect 2xx
        var responseString = await response.Content.ReadAsStringAsync();
        // MODIFIED: Deserialize to strongly-typed QuarterUpdateResponse DTO
        var apiResponse = JsonSerializer.Deserialize<QuarterUpdateResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(apiResponse);
        // MODIFIED: Access properties directly from the DTO
        Assert.Equal("Draft saved.", apiResponse.Message);
        Assert.Equal("DRAFT", apiResponse.Status);
        Assert.Equal(updateRequest.TaxableIncome, apiResponse.TaxableIncome);
        Assert.Equal(updateRequest.AllowableExpenses, apiResponse.AllowableExpenses);
        Assert.Equal(updateRequest.TaxableIncome - updateRequest.AllowableExpenses, apiResponse.NetProfit);
        // Verify ReplaceOneAsync was called on the mock with the correct updated data
        _factory.MockQuarterlyUpdatesCollection.Verify(
            c => c.ReplaceOneAsync(
                It.Is<FilterDefinition<QuarterlyUpdate>>(filter => filter != null),
                It.Is<QuarterlyUpdate>(q =>
                    q.Id == quarterToUpdateId &&
                    q.BusinessId == businessId &&
                    q.TaxableIncome == updateRequest.TaxableIncome &&
                    q.AllowableExpenses == updateRequest.AllowableExpenses &&
                    q.NetProfit == (updateRequest.TaxableIncome - updateRequest.AllowableExpenses) &&
                    q.Status == "DRAFT"),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(capturedUpdate);
        Assert.Equal(quarterToUpdateId, capturedUpdate.Id);
        Assert.Equal(updateRequest.TaxableIncome, capturedUpdate.TaxableIncome);
        Assert.Equal(updateRequest.AllowableExpenses, capturedUpdate.AllowableExpenses);
        Assert.Equal(updateRequest.TaxableIncome - updateRequest.AllowableExpenses, capturedUpdate.NetProfit);
        Assert.Equal("DRAFT", capturedUpdate.Status);
    }


    [Fact]
    public async Task SubmitQuarter_ChangesStatusAndAddsSubmissionDetails()
    {
        _factory.ResetDatabase();

        // Arrange
        var email = $"quarter_submit_{Guid.NewGuid()}@example.com";
        var username = "SubmitUser";
        var password = "SubmitPassword123!";
        var businessName = "Submit Business Plc.";
        var startDate = new DateTime(2025, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        var authResponse = await RegisterAndLoginUser(email, username, password);

        // Register a business to create initial DRAFT quarters
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var businessRequest = new BusinessRequest { Name = businessName, StartDate = startDate };
        var businessResponse = await _client.PostAsJsonAsync("/api/business", businessRequest);
        businessResponse.EnsureSuccessStatusCode();
        var businessContent = await businessResponse.Content.ReadAsStringAsync();
        var bizResponse = JsonSerializer.Deserialize<BusinessResponse>(businessContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(bizResponse);
        var businessId = bizResponse.BusinessId;

        // Manually create mock quarters with some data, one of which will be submitted
        var quarterToSubmitId = ObjectId.GenerateNewId().ToString(); // Generate a specific ID for the quarter we'll submit
        var initialQuarters = new List<QuarterlyUpdate>
        {
            new QuarterlyUpdate { Id = quarterToSubmitId, BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q1", Status = "DRAFT", TaxableIncome = 20000.00m, AllowableExpenses = 5000.00m, NetProfit = 15000.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q2", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q3", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m },
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q4", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m }
        };

        // Mock FindAsync to return the specific quarter we intend to submit
        var mockAsyncCursorFind = new Mock<IAsyncCursor<QuarterlyUpdate>>();
        mockAsyncCursorFind.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockAsyncCursorFind.SetupGet(_ => _.Current).Returns(new[] { initialQuarters.First(q => q.Id == quarterToSubmitId) });

        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAsyncCursorFind.Object);

        // Capture the updated quarter document that ReplaceOneAsync is called with
        QuarterlyUpdate? capturedSubmission = null;
        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<QuarterlyUpdate>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<QuarterlyUpdate>, QuarterlyUpdate, ReplaceOptions, CancellationToken>(
                (filter, update, options, token) => capturedSubmission = update)
            .ReturnsAsync(new Mock<ReplaceOneResult>().Object); // Return a successful mock result

        // Act
        var response = await _client.PostAsync($"/api/quarter/{quarterToSubmitId}/submit", null); // No request body for submit

        // Assert
        response.EnsureSuccessStatusCode(); // Expect 2xx
        var responseString = await response.Content.ReadAsStringAsync();
        // MODIFIED: Deserialize to strongly-typed QuarterSubmissionResponse DTO
        var apiResponse = JsonSerializer.Deserialize<QuarterSubmissionResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(apiResponse);
        // MODIFIED: Access properties directly from the DTO
        Assert.Equal("Quarter submitted successfully.", apiResponse.Message);
        Assert.Equal("SUBMITTED", apiResponse.Status);
        Assert.NotNull(apiResponse.SubmissionDetails);
        Assert.NotNull(apiResponse.SubmissionDetails.RefNumber);
        Assert.True(apiResponse.SubmissionDetails.SubmittedAt > DateTime.MinValue); // Ensure it's a valid date

        // Verify ReplaceOneAsync was called on the mock with the correct updated data
        _factory.MockQuarterlyUpdatesCollection.Verify(
            c => c.ReplaceOneAsync(
                It.Is<FilterDefinition<QuarterlyUpdate>>(filter => filter != null),
                It.Is<QuarterlyUpdate>(q =>
                    q.Id == quarterToSubmitId &&
                    q.BusinessId == businessId &&
                    q.Status == "SUBMITTED" &&
                    q.SubmissionDetails != null &&
                    !string.IsNullOrEmpty(q.SubmissionDetails.RefNumber)),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(capturedSubmission);
        Assert.Equal(quarterToSubmitId, capturedSubmission.Id);
        Assert.Equal("SUBMITTED", capturedSubmission.Status);
        Assert.NotNull(capturedSubmission.SubmissionDetails);
        Assert.False(string.IsNullOrEmpty(capturedSubmission.SubmissionDetails.RefNumber));
        Assert.True(capturedSubmission.SubmissionDetails.SubmittedAt > DateTime.MinValue); // Ensure it's a valid date
    }


    [Fact]
    public async Task CalculateCumulativeEstimatedTaxLiability_ForSubmittedQuarters()
    {
        _factory.ResetDatabase();

        // Arrange
        var email = $"tax_calc_{Guid.NewGuid()}@example.com";
        var username = "TaxCalcUser";
        var password = "TaxCalcPassword123!";
        var businessName = "Tax Calc Business";
        var startDate = new DateTime(2025, 4, 6, 0, 0, 0, DateTimeKind.Utc); // MTD fiscal year start

        var authResponse = await RegisterAndLoginUser(email, username, password);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        var businessRequest = new BusinessRequest { Name = businessName, StartDate = startDate };
        var businessResponse = await _client.PostAsJsonAsync("/api/business", businessRequest);
        businessResponse.EnsureSuccessStatusCode();
        var businessContent = await businessResponse.Content.ReadAsStringAsync();
        var bizResponse = JsonSerializer.Deserialize<BusinessResponse>(businessContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(bizResponse);
        var businessId = bizResponse.BusinessId;

        // Manually create mock quarters, with some submitted and having profit for tax calculation
        var mockQuarters = new List<QuarterlyUpdate>
        {
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q1", Status = "SUBMITTED", TaxableIncome = 25000.00m, AllowableExpenses = 5000.00m, NetProfit = 20000.00m }, // Net Profit: 20000
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q2", Status = "SUBMITTED", TaxableIncome = 10000.00m, AllowableExpenses = 1000.00m, NetProfit = 9000.00m },  // Net Profit: 9000
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q3", Status = "DRAFT", TaxableIncome = 100.00m, AllowableExpenses = 10.00m, NetProfit = 90.00m }, // DRAFT, should not count for submitted totals
            new QuarterlyUpdate { Id = ObjectId.GenerateNewId().ToString(), BusinessId = businessId, TaxYear = "2025/26", QuarterName = "Q4", Status = "DRAFT", TaxableIncome = 0.00m, AllowableExpenses = 0.00m, NetProfit = 0.00m }
        };

        // Mock MongoDB Find and ToListAsync for the /api/quarters endpoint
        var mockAsyncCursor = new Mock<IAsyncCursor<QuarterlyUpdate>>();
        mockAsyncCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockAsyncCursor.SetupGet(_ => _.Current).Returns(mockQuarters);

        _factory.MockQuarterlyUpdatesCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<QuarterlyUpdate>>(),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAsyncCursor.Object);

        // Act
        var response = await _client.GetAsync("/api/quarters");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var quartersResponse = JsonSerializer.Deserialize<QuartersResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(quartersResponse);

        // Expected Calculations (based on API logic: 20% tax above £12,570 personal allowance)
        // Total Net Profit from SUBMITTED quarters: 20000.00 + 9000.00 = 29000.00
        decimal expectedTotalNetProfitSubmitted = 29000.00m;
        // Taxable amount: 29000.00 - 12570.00 (personal allowance) = 16430.00
        // Cumulative Estimated Tax Liability: 16430.00 * 0.20 (20% tax rate) = 3286.00
        decimal expectedCumulativeEstimatedTaxLiability = 3286.00m;

        Assert.Equal(expectedTotalNetProfitSubmitted, quartersResponse.TotalNetProfitSubmitted);
        Assert.Equal(expectedCumulativeEstimatedTaxLiability, quartersResponse.CumulativeEstimatedTaxLiability);

        _factory.MockQuarterlyUpdatesCollection.Verify(
            c => c.FindAsync(
                It.Is<FilterDefinition<QuarterlyUpdate>>(filter => filter != null),
                It.IsAny<FindOptions<QuarterlyUpdate, QuarterlyUpdate>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
