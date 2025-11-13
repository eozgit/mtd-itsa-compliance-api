
# CodeCompanion Project Handover Document: MTD-ITSA Compliance Portal Starter

**Date:** 2025-11-12
**Author:** CodeCompanion (Previous Session)
**Project Lead:** Enis (User)

## 1. Project Overview

This project is a full-stack boilerplate for a **Making Tax Digital for Income Tax Self Assessment (MTD-ITSA) Compliance Portal**. It's designed to provide a robust starter kit with pre-built cross-cutting concerns to enable rapid development of core business logic. The backend uses ASP.NET Core Web API (C#), and the frontend is intended to be Angular (TypeScript).

## 2. Project Specification (`REQ_SPEC.md`)

The complete project specification, including goals, functional scope, technical stack requirements, API contracts (DTOs and Endpoints), hybrid database design (SQL Server for `Users`/`Businesses`, MongoDB for `QuarterlyUpdates`), frontend considerations, and system flows with Cucumber scenarios, is as follows:

```markdown
# Project Specification: MTD-ITSA Compliance Portal Starter

**Version:** 1.0
**Date:** 2025-11-12
**Author:** CodeCompanion

## 1. Introduction

This document outlines the requirements for a full-stack boilerplate for a **Making Tax Digital for Income Tax Self Assessment (MTD-ITSA) Compliance Portal**. The primary goal is to provide a robust starter kit with pre-built cross-cutting concerns to enable rapid development of core business logic.

## 2. Project Goals

*   Establish a full-stack project structure using the official Microsoft `.NET/Angular` starter template.
*   Implement secure user authentication (Registration and Login).
*   Provide a foundation for admin dashboard functionality and navigation.
*   Demonstrate core MTD functionality: submitting quarterly summaries of income and expenses.
*   Offer immediate value to the user through data enrichment based on submitted data.

## 3. Functional Scope

The application provides basic user management, business registration, and the core MTD functionality: submitting quarterly summaries of income and expenses.

### 3.1. Core Functionality

1.  **Authentication:** User registration and login.
2.  **Business Setup:** Registering a single self-employment business per user.
3.  **Quarterly Data Entry (R2):** Inputting aggregated Taxable Income and Allowable Expenses per quarter.
4.  **Quarterly Submission (S1):** Marking a quarter as submitted (simulated HMRC submission).

### 3.2. Data Enrichment

1.  **Net Profit/Loss (E1):** Calculate and display Net Profit/Loss for each quarter immediately upon data entry.
2.  **Cumulative Estimated Tax Liability (E2):** Calculate and display the cumulative estimated tax liability based on all submitted quarters.
3.  **Data Visualization (E3):** (Future consideration/Frontend task) Display trend comparison of Income vs. Expenses across quarters.

## 4. Technical Stack Requirements

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Backend (BE)** | **ASP.NET Core Web API (C#)** | Hosts the application, handles business logic, and manages data access. |
| **Frontend (FE)** | **Angular (TypeScript)** | Single Page Application (SPA) for the user interface. |
| **UI/Styling** | **Tailwind CSS** | Used for all styling. |
| **Architecture**| Official `.NET/Angular` Starter Template | Required structure for API proxy and hosting setup. |
| **SQL Database** | **SQL Server** | Primary database for relational data: `Users` and `Businesses`. |
| **NoSQL Database** | **MongoDB** | Document database for flexible document-style records: `QuarterlyUpdates`. |

## 5. API Contract

All endpoints require authentication (JWT/Token) after successful login. The backend will implement a mock JWT token generation and validation for initial development.

### 5.1. Data Transfer Objects (DTOs)

The following C# DTOs are required for API request and response bodies:

#### `RegisterRequest` DTO
*   `Email` (string, required)
*   `Password` (string, required)
*   `UserName` (string, required)

#### `LoginRequest` DTO
*   `Email` (string, required)
*   `Password` (string, required)

#### `AuthResponse` DTO
*   `UserId` (string, unique identifier)
*   `UserName` (string)
*   `Token` (string - mock JWT token)

#### `BusinessRequest` DTO
*   `Name` (string, required)
*   `StartDate` (DateTime, required - e.g., "YYYY-MM-DD")

#### `BusinessResponse` DTO
*   `BusinessId` (int)
*   `Name` (string)

#### `QuarterlyUpdateRequest` DTO
*   `TaxableIncome` (decimal)
*   `AllowableExpenses` (decimal)

#### `QuartersResponse` DTO
*   `Quarters` (List of `QuarterlyUpdate` objects)
*   `TotalNetProfitSubmitted` (decimal)
*   `CumulativeEstimatedTaxLiability` (decimal)

### 5.2. API Endpoints

| Endpoint | HTTP Method | Description | Request Body (Payload Spec) | Response Body (Success Spec) |
| :--- | :--- | :--- | :--- | :--- |
| `/api/auth/register` | `POST` | Creates a new user account. | `{"email": "string", "password": "string", "userName": "string"}` | `{"token": "JWT_TOKEN", "userId": "string", "userName": "string"}` |
| `/api/auth/login` | `POST` | Authenticates an existing user and returns an auth token. | `{"email": "string", "password": "string"}` | `{"token": "JWT_TOKEN", "userId": "string", "userName": "string"}` |
| `/api/business` | `POST` | Registers a new business for the authenticated user and initializes 4 fiscal quarters. | `{"name": "string", "startDate": "YYYY-MM-DDTHH:MM:SS"}` | `{"id": int, "name": "string"}` |
| `/api/quarters` | `GET` | Lists all quarters for the user's business, including cumulative financial summaries. | (None) | `{"quarters": [{"id": "string", "taxYear": "string", "quarterName": "string", "status": "string", "taxableIncome": float, "allowableExpenses": float, "netProfit": float, ...}], "totalNetProfitSubmitted": float, "cumulativeEstimatedTaxLiability": float}` |
| `/api/quarter/{id}` | `PUT` | **R2:** Saves/updates taxable income and allowable expense data for a quarter in DRAFT status. | `{"taxableIncome": float, "allowableExpenses": float}` | `{"id": "string", "businessId": int, "taxYear": "string", "quarterName": "string", "taxableIncome": float, "allowableExpenses": float, "netProfit": float, "status": "DRAFT", "message": "Draft saved."}` |
| `/api/quarter/{id}/submit` | `POST` | **S1/S2:** Marks a quarter as submitted (simulated). Only quarters in 'DRAFT' status can be submitted. | (None) | `{"id": "string", "businessId": int, "taxYear": "string", "quarterName": "string", "taxableIncome": float, "allowableExpenses": float, "netProfit": float, "status": "SUBMITTED", "submissionDetails": {"refNumber": "MTD-ACK-...", "submittedAt": "datetime"}, "message": "Quarter submitted successfully."}` |

## 6. Hybrid Database Design

### 6.1. SQL Schema (Entity Framework Core)

| Table | Purpose | Fields |
| :--- | :--- | :--- |
| `Users` | Authentication & User Identity | `Id` (PK, string), `Email` (Unique, string), `UserName` (string), `PasswordHash` (string), `CreatedAt` (DateTime, auto) |
| `Businesses` | Business Metadata | `Id` (PK, int), `UserId` (FK to `Users.Id`, string), `Name` (string), `StartDate` (DateTime), `CreatedAt` (DateTime, auto) |

### 6.2. NoSQL Schema (MongoDB)

| Collection | Document Key Field | Purpose | Document Structure (Example) |
| :--- | :--- | :--- | :--- |
| `quarterly_updates` | `Id` (Unique ID, string) | Stores all MTD submission data for a single quarter. | `_id: "q1-2025-b101"`, `BusinessId: 101` (Index), `TaxYear: "2025/26"`, `QuarterName: "Q1"`, `TaxableIncome: 15000.00`, `AllowableExpenses: 4500.00`, `NetProfit: 10500.00` (Calculated), `Status: "SUBMITTED"`, `SubmissionDetails: { RefNumber: "MTD-ACK-...", SubmittedAt: "datetime"}` |

## 7. Frontend Considerations

The backend must support an Angular frontend adhering to a specific UI/UX structure. Key frontend pages and their corresponding backend interactions include:

*   **Login Screen (`/auth`):** Posts to `/api/auth/login`. Requires `AuthResponse` for user details.
*   **Register Screen (`/auth`):** Posts to `/api/auth/register`.
*   **Business Setup Screen (`/setup`):Ğ Posts to `/api/business`.
*   **Dashboard (`/dashboard`):** Displays a list of fiscal quarters, their status, income, expenses, net profit, and cumulative financial summaries. Fetches data from `/api/quarters`.
*   **Quarterly Entry (`/quarter/:id`):** Form for Taxable Income and Allowable Expenses. Buttons for "Save Draft" (PUT `/api/quarter/{id}`) and "Submit to HMRC" (POST `/api/quarter/{id}/submit`).
*   **Header:** Displays the authenticated user's name (`userName` from `AuthResponse`).
*   **Sidebar Navigation:** Links to Dashboard, Businesses, Users, and Settings (not all fully implemented in current scope).

## 8. System Flows and Cucumber Scenarios

### 8.1. Flow 1: User Registration and Authentication (A1)

**Flow Story:**

1.  User fills form with email, username, and password on the auth page and clicks Register.
2.  Frontend posts data to endpoint `/api/auth/register`.
3.  **Backend Logic:**
    *   Generates a `PasswordHash`.
    *   **SQL Write:** Inserts a new row into the `Users` table.
    *   Generates a **mock JWT token** for the session.
    *   **Returns:** HTTP 200 OK with the JWT token, user ID, and username.
4.  Frontend displays: Redirects to the `/setup` page (Business Setup).

**Cucumber Scenario (A1):**

````gherkin
Scenario: Successful user registration
Given the user is on the registration page
When the user enters "test@example.com" into the email field
And the user enters "TestUser" into the username field
And the user enters "secure-password-123" into the password field
And the user clicks the "Register" button
Then the application makes a POST request to "/api/auth/register" with the credentials
And the application receives a 200 status code with a JWT token, user ID, and username
And the user is redirected to the "/setup" page
````

### 8.2. Flow 2: Business Registration (B1)

**Flow Story:**

1.  User fills form with Business Name and Start Date on the `/setup` page and clicks Save Business Details.
2.  Frontend posts data to endpoint `/api/business` (including the JWT token in the Authorization header).
3.  **Backend Logic:**
    *   Validates the user token and start date using `AuthAndBusinessFilter`.
    *   **SQL Write:** Inserts a new row into the `Businesses` table, linked to the authenticated `UserId`.
    *   **Initializes Quarters:** Calculates the 4 fiscal quarters for the tax year starting from the provided `StartDate`.
    *   **NoSQL Writes:** Creates 4 initial documents in the `quarterly_updates` collection (one for each quarter) with `Status: DRAFT`, and `TaxableIncome`, `AllowableExpenses`, `NetProfit` set to `0.00`.
    *   **Returns:** HTTP 201 CREATED with the new business ID and name.
4.  Frontend displays: Redirects to the `/dashboard` page.

**Cucumber Scenario (B1):**

````gherkin
Scenario: Successful business registration and quarter initialization
Given the user is authenticated as user "42" and is on the "/setup" page
When the user enters "The Tech Emporium" into the Business Name field
And the user selects "2025-04-06" as the Accounting Start Date
And the user clicks the "Save Business Details" button
Then the application makes a POST request to "/api/business" with the business details and auth token
And the application receives a 201 status code with a business ID and name
And the backend has created 4 initial documents in the 'quarterly_updates' NoSQL collection with 'DRAFT' status
And the user is redirected to the "/dashboard" page
````

## 3. Current Project Structure

The project is located at `/home/enis/Work/tax2`. It currently has two main project folders as siblings:

```
/home/enis/Work/tax2/
├── api/                  # Main ASP.NET Core Web API project
│   ├── api.csproj
│   ├── Program.cs
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs
│   │   ├── BusinessEndpoints.cs
│   │   └── QuarterlyUpdateEndpoints.cs
│   └── ... other api files (Models, Data, Filters, etc.) ...
└── api.Tests.Integration/ # Integration test project
    ├── api.Tests.Integration.csproj
    ├── AuthIntegrationTests.cs
    └── CustomWebApplicationFactory.cs
```

## 4. Technical Stack

*   **Backend:** ASP.NET Core Web API (C#), .NET 9.0
*   **Frontend (Planned):** Angular (TypeScript)
*   **UI/Styling:** Tailwind CSS
*   **SQL Database:** SQL Server (using Entity Framework Core)
*   **NoSQL Database:** MongoDB (using MongoDB.Driver)
*   **Testing:** Xunit, Microsoft.AspNetCore.Mvc.Testing, Moq

## 5. Codebase Status

All provided files have been reviewed and modified to enable integration testing and address identified issues.

### 5.1. `api/api.csproj`

*   Updated `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` package versions to `9.0.0-preview.7.24406.2` for consistency with the test project's resolved dependencies.

~
````xml
<!-- filepath: api/api.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0-preview.7.24406.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0-preview.7.24406.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="MongoDB.Driver" Version="2.23.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

</Project>
````
~

### 5.2. `api/Program.cs`

*   The `public partial class Program { }` declaration has been added at the end of the file to make the `Program` class accessible to integration tests via `WebApplicationFactory`.
*   Swagger/OpenAPI configuration has been added.

~
````csharp
// filepath: api/Program.cs
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using api.Endpoints;
using api.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MTD-ITSA API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure SQL Server with Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(settings.DatabaseName);
    return database.GetCollection<QuarterlyUpdate>(settings.QuarterlyUpdatesCollectionName);
});

// Register the custom endpoint filter
builder.Services.AddScoped<AuthAndBusinessFilter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    // app.UseSwaggerUI() can be added here if a UI is desired, but not in spec.
}

app.UseHttpsRedirection();

// Map Endpoints using extension methods
app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapQuarterlyUpdateEndpoints();

app.Run();

public partial class Program { }
````
~

### 5.3. `api/Endpoints/AuthEndpoints.cs`

*   No recent functional changes. Contains `MapAuthEndpoints` for `/api/auth/register` and `/api/auth/login`, and a helper for mock JWT token generation/extraction.

### 5.4. `api/Endpoints/BusinessEndpoints.cs`

*   No recent functional changes. Contains `MapBusinessEndpoints` for `/api/business` (POST) with `AuthAndBusinessFilter` applied, and logic for generating fiscal quarters and inserting initial `QuarterlyUpdate` documents into MongoDB.

### 5.5. `api/Endpoints/QuarterlyUpdateEndpoints.cs`

*   No recent functional changes. Contains `MapQuarterlyUpdateEndpoints` for `/api/quarters` (GET), `/api/quarter/{id}` (PUT), and `/api/quarter/{id}/submit` (POST) with `AuthAndBusinessFilter` applied. Includes logic for calculating net profit and cumulative tax liability.

### 5.6. `api.Tests.Integration/api.Tests.Integration.csproj`

*   The `ProjectReference` to the main `api` project has been corrected to `..\api\api.csproj` to reflect the sibling folder structure.
*   `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory` package versions have been updated to `9.0.0-preview.7.24406.2` for consistency.

~
````xml
<!-- filepath: api.Tests.Integration/api.Tests.Integration.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0-preview.7.24406.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0-preview.7.24406.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\api\api.csproj" />
  </ItemGroup>

</Project>
````
~

### 5.7. `api.Tests.Integration/CustomWebApplicationFactory.cs`

*   A `ResetDatabase()` method has been added. This method is crucial for per-test isolation:
    *   It ensures the in-memory SQL database is completely dropped and recreated.
    *   It calls `MockQuarterlyUpdatesCollection.Reset()` to clear all `Moq` setups for the MongoDB collection.
*   The `ConfigureWebHost` method no longer performs initial data clearing, as `ResetDatabase()` handles this per-test.

~
````csharp
// filepath: api.Tests.Integration/CustomWebApplicationFactory.cs
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public Mock<IMongoCollection<QuarterlyUpdate>> MockQuarterlyUpdatesCollection { get; private set; } = null!;

    public void ResetDatabase()
    {
        using (var scope = Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            MockQuarterlyUpdatesCollection.Reset();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            services.RemoveAll(typeof(IMongoClient));
            services.RemoveAll(typeof(IMongoDatabase));
            services.RemoveAll(typeof(IMongoCollection<QuarterlyUpdate>));

            MockQuarterlyUpdatesCollection = new Mock<IMongoCollection<QuarterlyUpdate>>();
            services.AddSingleton(MockQuarterlyUpdatesCollection.Object);

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

                db.Database.EnsureCreated();
            }
        });

        builder.UseEnvironment("Development");
    }
}
````
~

### 5.8. `api.Tests.Integration/AuthIntegrationTests.cs`

*   Each test method now calls `_factory.ResetDatabase()` at the beginning to ensure a clean state for every test run.

~
````csharp
// filepath: api.Tests.Integration/AuthIntegrationTests.cs
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
using Microsoft.AspNetCore.Hosting;

namespace api.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Successful_User_Registration_Returns_Token_And_CreatesUser()
    {
        _factory.ResetDatabase();

        var email = $"register_success_{Guid.NewGuid()}@example.com";
        var username = "TestUserSuccess";
        var password = "SecurePassword123!";

        var registerRequest = new RegisterRequest
        {
            Email = email,
            UserName = username,
            Password = password
        };
        var content = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register", content);

        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrEmpty(authResponse.Token));
        Assert.False(string.IsNullOrEmpty(authResponse.UserId));
        Assert.Equal(username, authResponse.UserName);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            Assert.NotNull(user);
            Assert.Equal(registerRequest.Email, user.Email);
            Assert.Equal(registerRequest.UserName, user.UserName);
            Assert.False(string.IsNullOrEmpty(user.PasswordHash));
        }
    }

    [Fact]
    public async Task Register_With_Existing_Email_Returns_Conflict()
    {
        _factory.ResetDatabase();

        var email = $"register_duplicate_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            UserName = "DuplicateUser",
            Password = "SecurePassword123!"
        };
        var content = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", content);

        var duplicateContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/register", duplicateContent);

        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var errorResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains("User with this email already exists.", errorResponse);
    }

    [Fact]
    public async Task Successful_User_Login_Returns_Token()
    {
        _factory.ResetDatabase();

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
        await _client.PostAsync("/api/auth/register", registerContent);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", loginContent);

        response.EnsureSuccessStatusCode();
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
        _factory.ResetDatabase();

        var loginRequest = new LoginRequest
        {
            Email = $"nonexistent_{Guid.NewGuid()}@example.com",
            Password = "WrongPassword123!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", loginContent);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
````
~

## 6. Development Guidelines

*   **Markdown Formatting:** Use Markdown for all non-code text responses.
*   **Code Blocks:**
    *   Start and end code blocks with four backticks (````).
    *   Specify the programming language after the opening four backticks (e.g., ````csharp`).
    *   For changes to existing files, include `// filepath: /path/to/file` as a line comment.
    *   Use `// ...existing code...` to indicate unchanged code.
    *   Ensure line comments use the correct syntax for the language (e.g., `//` for C#, `#` for Python).
*   **Yankability:** Print a tilda (~) on a new line before and after each code block to facilitate easy yanking with `yi~`.
*   **Conciseness:** Keep answers short and impersonal.
*   **Step-by-step Approach:** Unless the task is very simple or specified otherwise, describe the plan in pseudocode.
*   **Focus on Task:** Only include relevant code and information.

## 7. Current Task & Next Steps

**Previous Goal:** Verify existing integration tests and then cover remaining APIs.
**Current State:** Encountered build failures when attempting to run `dotnet test`, specifically `NETSDK1064: Package Microsoft.Extensions.Configuration.Binder... was not found.` and other compilation errors related to test assets being compiled with the main project.

**Diagnosis:** The primary cause of the `NETSDK1064` error and the build failures for the `api` project was likely due to the `api.Tests.Integration` project being incorrectly placed as a subfolder of `api`. This was addressed by instructing the user to move the `api.Tests.Integration` folder to be a sibling of `api`, and then updating the project reference in `api.Tests.Integration.csproj`. The package versions were also harmonized. However, the last `dotnet clean` and `dotnet restore` commands issued by the user were not comprehensive enough, leading to the continued `NETSDK1064` error.

**Next Action for the AI Assistant:**

The immediate next step is to ensure the project builds and all existing `AuthIntegrationTests` pass. This requires a thorough clean, restore, build, and test cycle, explicitly targeting each project, to correctly resolve all NuGet dependencies and compile everything.

1.  **Verify Project Structure:** Confirm the `/home/enis/Work/tax2/` directory contains `api/` and `api.Tests.Integration/` as sibling folders.
2.  **Verify File Contents:** Double-check that `api/api.csproj`, `api/Program.cs`, and `api.Tests.Integration/api.Tests.Integration.csproj` match the latest provided content in this handover document.
3.  **Execute Comprehensive Build/Test Commands:** Guide the user through the following commands, ensuring they are executed from the **solution root (`/home/enis/Work/tax2`)**:

    ```bash
    # Ensure all projects are clean
    dotnet clean api/api.csproj
    dotnet clean api.Tests.Integration/api.Tests.Integration.csproj

    # Clear all NuGet caches
    dotnet nuget locals all --clear

    # Restore packages for the main API project
    dotnet restore api/api.csproj

    # Restore packages for the integration test project
    dotnet restore api.Tests.Integration/api.Tests.Integration.csproj

    # Build the main API project
    dotnet build api/api.csproj

    # Build the integration test project (this will also build the main project if needed)
    dotnet build api.Tests.Integration/api.Tests.Integration.csproj

    # Run the integration tests
    dotnet test api.Tests.Integration/api.Tests.Integration.csproj
    ```
4.  **Confirm Test Pass:** Once the `dotnet test` command is successful and all `AuthIntegrationTests` pass, proceed to the original plan: **write new integration tests for the Business and Quarterly Update endpoints.** These new tests should adhere to the `ResetDatabase()` pattern and correctly set up `Moq` expectations for MongoDB interactions.
