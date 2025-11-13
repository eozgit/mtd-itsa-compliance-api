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
*   **Business Setup Screen (`/setup`):** Form for Business Name and Accounting Start Date. Posts to `/api/business`.
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
