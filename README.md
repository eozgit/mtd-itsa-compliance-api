
# MTD-ITSA Compliance Portal Starter (Backend API)

## 1. Project Overview

This project serves as a robust backend API boilerplate for a **Making Tax Digital for Income Tax Self Assessment (MTD-ITSA) Compliance Portal**. Its primary goal is to provide a solid foundation with essential, pre-built cross-cutting concerns, enabling rapid development of core business logic.

While the full vision includes an Angular frontend (not part of this backend project), this API provides all necessary endpoints for user authentication, business registration, quarterly data entry, submission, and basic data enrichment.

## 2. Technical Stack (Backend)

*   **Platform**: ASP.NET Core Web API (C#) - .NET 9.0
*   **Web Framework**: Minimal APIs
*   **Relational Database**: SQL Server (via Entity Framework Core)
*   **Document Database**: MongoDB (for flexible, schema-less quarterly updates)
*   **Authentication**: Mock JWT Bearer Token (placeholder for full JWT implementation)

## 3. Core Functionality & Features Implemented

Based on the project specifications (`mtd-itsa.md` and `mtd2.md`), the following features are implemented and functional:

*   **User Authentication**:
    *   User Registration (`POST /api/auth/register`)
    *   User Login (`POST /api/auth/login`)
*   **Business Setup**:
    *   Registration of a single self-employment business per user (`POST /api/business`).
    *   Automatic generation of 4 fiscal quarters (starting from the business's `StartDate`) in MongoDB upon business registration.
*   **Quarterly Data Management**:
    *   Retrieval of all quarterly updates for a user's business (`GET /api/quarters`).
    *   Saving/updating income and expense data for a specific quarter (`PUT /api/quarter/{id}`). Quarters must be in `DRAFT` status to be updated.
    *   Submitting a quarter (`POST /api/quarter/{id}/submit`), changing its status from `DRAFT` to `SUBMITTED` and generating submission details.
*   **Data Enrichment**:
    *   **Net Profit/Loss Calculation**: Automatically calculated (`TaxableIncome - AllowableExpenses`) during quarterly updates.
    *   **Cumulative Estimated Tax Liability**: Calculated for all `SUBMITTED` quarters when retrieving `/api/quarters` (currently uses a hardcoded 20% tax rate).
*   **Hybrid Database Approach**: SQL Server for relational user and business metadata, MongoDB for flexible quarterly submission records.
*   **Centralized Authorization & Business Lookup**: An `IEndpointFilter` is implemented to handle token validation, user ID extraction, and business lookup, reducing boilerplate in endpoint handlers.

## 4. API Endpoints (Contract)

All endpoints requiring authentication expect a `Bearer {token}` in the `Authorization` header.

| Endpoint | HTTP Method | Description | Request Body Example | Response Body Example |
| :--- | :--- | :--- | :--- | :--- |
| `/api/auth/register` | `POST` | Creates a new user account. | `{"email": "string", "password": "string", "userName": "string"}` | `{"userId": "uuid", "userName": "string", "token": "mock-jwt-token"}` |
| `/api/auth/login` | `POST` | Authenticates a user, returns mock JWT. | `{"email": "string", "password": "string"}` | `{"userId": "uuid", "userName": "string", "token": "mock-jwt-token"}` |
| `/api/business` | `POST` | Registers a new business for the authenticated user. Automatically initializes 4 fiscal quarters. | `{"name": "string", "startDate": "YYYY-MM-DDTHH:MM:SS"}` | `{"businessId": int, "name": "string"}` |
| `/api/quarters` | `GET` | Lists all quarters for the user's business, including cumulative tax liability. | (None) | `{ "quarters": [...], "cumulativeEstimatedTaxLiability": decimal, "totalNetProfitSubmitted": decimal }` |
| `/api/quarter/{id}` | `PUT` | Saves/updates income/expense data for a DRAFT quarter. | `{"taxableIncome": decimal, "allowableExpenses": decimal}` | `{"id": "string", "businessId": int, "taxYear": "string", "quarterName": "string", "taxableIncome": decimal, "allowableExpenses": decimal, "netProfit": decimal, "status": "DRAFT", "message": "Draft saved."}` |
| `/api/quarter/{id}/submit` | `POST` | Marks a DRAFT quarter as SUBMITTED. | (None) | `{"id": "string", "businessId": int, "taxYear": "string", "quarterName": "string", "taxableIncome": decimal, "allowableExpenses": decimal, "netProfit": decimal, "status": "SUBMITTED", "submissionDetails": {"refNumber": "string", "submittedAt": "datetime"}, "message": "Quarter submitted successfully."}` |

## 5. Backend Architecture & Key Decisions

*   **Minimal API Paradigm**: Utilized for lightweight, focused HTTP API endpoints. This keeps the codebase lean and performance-oriented.
*   **Hybrid Database Strategy**:
    *   **SQL Server (EF Core)**: Manages `Users` and `Businesses` due to their relational nature and the need for strong data integrity (e.g., foreign keys, unique constraints).
    *   **MongoDB**: Stores `QuarterlyUpdate` documents. This offers schema flexibility, which is advantageous for tax-related data that might evolve or have varied reporting requirements over time without rigid schema migrations.
*   **Centralized Authorization and Business Lookup (`IEndpointFilter`)**:
    *   The `AuthAndBusinessFilter` class (`Filters/AuthAndBusinessFilter.cs`) abstracts the common logic of extracting the `currentUserId` from the mock JWT token and retrieving the associated `Business` object from the database.
    *   This pattern reduces code duplication across multiple endpoints in `BusinessEndpoints.cs` and `QuarterlyUpdateEndpoints.cs`, improving maintainability and ensuring consistent authorization checks.
*   **Mock JWT Implementation**: For rapid development and POC purposes, a simple string-based mock JWT token is used. This allows frontend integration without complex security infrastructure during the initial phase.

## 6. Database Setup

### SQL Server

1.  **Connection String**: Configure your SQL Server connection in `appsettings.json` or `appsettings.Development.json`.
    ```json
    // filepath: appsettings.Development.json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=mtditsa;User Id=SA;Password=55555Percent;TrustServerCertificate=True"
      }
    }
    ```
2.  **Migrations**: Apply Entity Framework Core migrations to create the `Users` and `Businesses` tables.
    ```bash
    dotnet ef database update
    ```

### MongoDB

1.  **Connection String**: Configure your MongoDB connection in `appsettings.json` or `appsettings.Development.json`.
    ```json
    // filepath: appsettings.Development.json
    {
      "MongoDbSettings": {
        "ConnectionString": "mongodb://localhost:27017",
        "DatabaseName": "mtditsa_quarters",
        "QuarterlyUpdatesCollectionName": "quarterly_updates"
      }
    }
    ```
2.  No schema setup is strictly required beforehand for MongoDB; collections and documents are created on first insert.

## 7. Project Setup & Running

### Prerequisites

*   .NET SDK 9.0 (or compatible version)
*   SQL Server instance (local or remote)
*   MongoDB instance (local or remote, e.g., Docker container `docker run -d -p 27017:27017 --name mongo-db mongo`)
*   `jq` (for parsing JSON in the bash script - `sudo apt install jq`)
*   `uuidgen` (for generating unique IDs in the bash script - usually pre-installed)

### Backend Setup Steps

1.  **Clone the repository**:
    ```bash
    git clone <repository-url>
    cd api # Navigate to the backend project directory
    ```
2.  **Restore dependencies**:
    ```bash
    dotnet restore
    ```
3.  **Build the project**:
    ```bash
    dotnet build
    ```
4.  **Apply SQL database migrations** (as described in Section 6.1).
5.  **Run the API**:
    ```bash
    dotnet run
    ```
    The API will typically run on `http://localhost:5129` (HTTP) and `https://localhost:7042` (HTTPS) as configured in `Properties/launchSettings.json`.

### Testing the Full Flow with `test_full_flow.sh`

A bash script (`test_full_flow.sh`) is provided to automate the entire API workflow, from user registration to quarterly submission.

1.  **Make the script executable**:
    ```bash
    chmod +x test_full_flow.sh
    ```
2.  **Execute the script**:
    ```bash
    ./test_full_flow.sh
    ```
    This script will:
    *   Generate a unique test user.
    *   Register and log in the user, capturing the mock JWT token.
    *   Register a business for the user.
    *   Retrieve the generated fiscal quarters, extracting the ID of the last quarter.
    *   Update the last quarter's income and expenses.
    *   Submit the last quarter.
    *   Output the responses at each step.

## 8. Future Work & Enhancements

*   **Implement Real JWT Authentication**: Replace the mock JWT with a production-ready JWT implementation (e.g., using `Microsoft.AspNetCore.Authentication.JwtBearer` with proper key management).
*   **Integrate ASP.NET Core Identity**: For robust user management, password hashing, and role-based authorization.
*   **Configure Tax Rate**: Move the hardcoded `0.20m` tax rate for cumulative tax liability into `appsettings.json` for easier configuration.
*   **Input Validation**: Add more comprehensive server-side input validation (e.g., using data annotations or FluentValidation) to DTOs.
*   **Comprehensive Error Handling**: Implement global exception handling middleware or more granular error responses.
*   **Unit/Integration Tests**: Develop a suite of automated tests for critical business logic and endpoint functionality.
*   **Frontend Integration**: Develop the Angular frontend as specified in the project requirements.
*   **Data Visualization (E3)**: Implement backend logic (if any specific aggregation is needed beyond current) and frontend components for trend comparison of Income vs. Expenses.
