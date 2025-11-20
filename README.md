
# MTD-ITSA Compliance Portal

This repository contains the full-stack boilerplate for a **Making Tax Digital for Income Tax Self Assessment (MTD-ITSA) Compliance Portal**. It provides a robust starter kit with pre-built cross-cutting concerns to enable rapid development of core business logic and an integrated Angular frontend.

## 1. Project Overview

The MTD-ITSA Compliance Portal aims to simplify tax reporting for self-employed individuals by allowing them to input, manage, and submit their quarterly income and expenses. The application supports user authentication, business registration, quarterly data management, and basic financial data enrichment (Net Profit/Loss, Estimated Tax Liability).

## 2. Technical Stack

This project is built using a modern full-stack approach:

*   **Backend:** ASP.NET Core Web API (C# .NET 9.0) with Minimal APIs
    *   **SQL Database:** SQL Server (for Users and Businesses metadata)
    *   **NoSQL Database:** MongoDB (for flexible Quarterly Update documents)
    *   **Authentication:** Mock JWT Bearer Token
*   **Frontend:** Angular (TypeScript)
    *   **UI/Styling:** Tailwind CSS

## 3. Core Functionality

*   User Registration and Login
*   Single Self-Employment Business Registration per user, with automatic quarter initialization.
*   Entry and Saving of Taxable Income and Allowable Expenses for quarters.
*   Submission of quarters (changing status from DRAFT to SUBMITTED).
*   Automatic calculation of Net Profit/Loss per quarter.
*   Cumulative Estimated Tax Liability calculation for submitted quarters.
*   Data visualization for financial trends (Frontend task).

## 4. Getting Started

To set up and run the full-stack application, follow these steps.

### Prerequisites

*   .NET SDK 9.0 (or compatible version)
*   Node.js (LTS version) & npm (for frontend development, if contributing there)
*   Angular CLI (`npm install -g @angular/cli`) (for frontend development, if contributing there)
*   SQL Server instance (local or remote)
*   MongoDB instance (local or remote, e.g., Docker container)
    *   `docker run -d -p 27017:27017 --name mongo-db mongo`

### 4.1. Backend Setup

Navigate to the `api` directory for detailed backend setup instructions.

```bash
cd api
```

Please refer to the [Backend README](./api/README.md) for specifics on:
*   Configuring SQL Server and MongoDB connection strings (`appsettings.Development.json`).
*   Applying Entity Framework Core migrations.
*   Running the backend API.

### 4.2. Frontend Setup

The Angular frontend for this project is hosted in a separate repository.

Please refer to the [Frontend Repository](https://github.com/eozgit/mtd-itsa-compliance-fe) for setup instructions, development server details, API client generation, and testing.

### 4.3. Running the Full Stack

To run the full stack, you will need to:
1.  Start the backend API (see section 4.1).
2.  Deploy or run the frontend application separately (see section 4.2 and its respective repository).
3.  Ensure your frontend is configured to point to the backend API's address (e.g., `http://localhost:5129`).

## 5. API Documentation

The backend exposes an OpenAPI (Swagger) specification:
*   **JSON:** `http://localhost:5129/swagger/v1/swagger.json`

## 6. Testing

*   **Backend Integration Tests:** Located in `api.Tests.Integration/`.
    ```bash
    dotnet test api.Tests.Integration/api.Tests.Integration.csproj
    ```

## 7. Project Structure

```
.
├── api/                   # ASP.NET Core Web API project (Backend)
│   ├── api.csproj
│   ├── Program.cs
│   ├── Endpoints/         # Defines API endpoints (Auth, Business, QuarterlyUpdate)
│   ├── Filters/           # Custom endpoint filters (AuthAndBusinessFilter)
│   ├── Models/            # Data Transfer Objects (DTOs) and database models
│   ├── Data/              # Database context (SQL) and MongoDB configuration
│   └── ... other backend files ...
├── api.Tests.Integration/ # Integration test project for the backend API
│   ├── api.Tests.Integration.csproj
│   ├── CustomWebApplicationFactory.cs
│   └── ... integration test files ...
├── .gitignore
├── README.md              # This file - high-level project overview
├── test_full_flow.sh      # Bash script to test the full API flow
└── ... other configuration files ...

