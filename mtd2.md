# MTD for ITSA - Simplified POC Specification

This document outlines the scope, technical contracts, and functional flows for a Proof of Concept (POC) demonstrating the core quarterly submission requirement of Making Tax Digital for Income Tax Self Assessment (MTD for ITSA).

## 1\. Scope & Enrichment

The application provides basic user management, business registration, and the core MTD functionality: submitting quarterly summaries of income and expenses. The enrichment features provide immediate value to the user based on the submitted data.

### Functional Scope

1.  **Authentication:** User registration and login.
2.  **Business Setup:** Registering a single self-employment business.
3.  **Quarterly Data Entry (R2):** Inputting aggregated Taxable Income and Allowable Expenses per quarter.
4.  **Quarterly Submission (S1):** Marking a quarter as submitted (simulated HMRC submission).
5.  **Data Enrichment (E1):** Calculate and display the **Net Profit/Loss** immediately.
6.  **Data Enrichment (E2):** Calculate and display the **Cumulative Estimated Tax Liability** based on all submitted quarters.
7.  **Data Visualization (E3):** Display trend comparison of Income vs. Expenses across quarters.

## 2\. Frontend Pages & UI Elements

The frontend requires four main views:

| Page | Route | UI Elements, Fields, & Buttons |
| :--- | :--- | :--- |
| **Login** | `/auth` | Form: Email (input), Password (input). Buttons: Login, Register. Link: Switch to Registration/Login. |
| **Register** | `/auth` | |
| **Business Setup** | `/setup` | Form: Business Name (input), Accounting Start Date (date input, e.g., 2025-04-06). Button: Save Business Details. |
| **Dashboard** | `/dashboard` | List: Fiscal quarters for the current year (Q1, Q2, Q3, Q4). Data Display (per quarter): Status (Draft/Submitted), Income, Expenses, Net Profit/Loss. Cumulative Display: Total Income, Total Expenses, Total Estimated Tax Due (based on cumulative profit). Buttons (per quarter): Enter Data (if Draft), View Submission (if Submitted). |
| **Quarterly Entry** | `/quarter/:id` | Form Fields: Taxable Income (£) (number input), Allowable Expenses (£) (number input). Buttons: Save Draft, Submit to HMRC (Simulated). Display (if submitted): HMRC Acknowledgement Reference, Submission Date. |

## 3\. Consolidated API Contract

All endpoints require authentication (JWT/Token) after successful login.

| Endpoint | HTTP Method | Description | Request Body (Payload Spec) | Response Body (Success Spec) |
| :--- | :--- | :--- | :--- | :--- |
| **/api/auth/register** | `POST` | Creates a new user. | `{"email": "string", "password": "string", "user_name": "string"}` | `{"token": "JWT_TOKEN", "user_id": 1, "user_name": "string"}` |
| **/api/auth/login** | `POST` | Logs in user and returns auth token. | `{"email": "string", "password": "string"}` | `{"token": "JWT_TOKEN", "user_id": 1, "user_name": "string"}` |
| **/api/business** | `POST` | Registers a new business for the user. | `{"name": "string", "start_date": "YYYY-MM-DD"}` | `{"business_id": 101, "name": "Sole Trader Ltd"}` |
| **/api/quarters** | `GET` | Lists all quarters for the user's business. | (None) | `[{ "quarter_id": "string", "name": "Q1 2025/26", "status": "string", "income": float, "profit": float, ...}]` |
| **/api/quarter/{id}** | `PUT` | **R2:** Saves/updates income/expense data. | `{"taxable_income": float, "allowable_expenses": float}` | `{"quarter_id": "string", "status": "DRAFT", "net_profit": float, "message": "Draft saved."}` |
| **/api/quarter/{id}/submit** | `POST` | **S1/S2:** Marks quarter as submitted (simulated). | (None) | `{"quarter_id": "string", "status": "SUBMITTED", "hmrc_ack_ref": "MTD-ACK-...", "submitted_at": "datetime"}` |

## 4\. Hybrid Database Design (SQL + NoSQL)

We use SQL for relational integrity (Users and Business metadata) and NoSQL for the flexible document-style quarterly records.

### SQL Schema (Primary Database: PostgreSQL/MYSQL)

| Table | Purpose | Fields |
| :--- | :--- | :--- |
| `users` | Authentication & User Identity | `id` (PK), `email` (Unique), `password_hash`, `created_at` |
| `businesses` | Business Metadata | `id` (PK), `user_id` (FK to users.id), `name`, `start_date`, `created_at` |

### NoSQL Schema (Document Database: MongoDB/Firestore)

| Collection | Document Key Field | Purpose | Document Structure (Example) |
| :--- | :--- | :--- | :--- |
| `quarterly_updates` | `_id` (Unique ID) | Stores all MTD submission data for a single quarter. | `_id: "q1-2025-b101"`, `business_id: 101` (Index), `tax_year: "2025/26"`, `quarter_name: "Q1"`, `taxable_income: 15000.00`, `allowable_expenses: 4500.00`, `net_profit: 10500.00` (Calculated), `status: "SUBMITTED"`, `submission_details: { ref_number: "MTD-ACK-...", submitted_at: "datetime"}` |

## 5\. System Flows and Cucumber Scenarios

### Flow 1: User Registration and Authentication (A1)

**Flow Story:**

1.  User fills form with email and password on the auth page and clicks Register.
2.  FE posts data to endpoint `/api/auth/register`.
      * **Payload Spec:** `{"email": "john@example.com", "password": "secure-password-123"}`
3.  **BE logic:**
      * Generates a `password_hash`.
      * **SQL Write:** Inserts a new row into the `users` table.
      * Generates a **JWT token** for the session.
      * **Returns:** HTTP 200 OK with the JWT token and user ID.
      * **Response Spec:** `{"token": "eyJ...", "user_id": 42, "user_name": "John Doe"}`
4.  FE displays: Redirects to the `/setup` page (Business Setup).
5.  User is happy.

**Cucumber Scenario (A1):**

```gherkin
Scenario: Successful user registration
Given the user is on the registration page
When the user enters "test@example.com" into the email field
And the user enters "secure-password-123" into the password field
And the user clicks the "Register" button
Then the application makes a POST request to "/api/auth/register" with the credentials
And the application receives a 200 status code with a JWT token
And the user is redirected to the "/setup" page
```

### Flow 2: Business Registration (B1)

**Flow Story:**

1.  User fills form with Business Name and Start Date on the `/setup` page and clicks Save Business Details.
2.  FE posts data to endpoint `/api/business` (including the JWT token in the header).
      * **Payload Spec:** `{"name": "My Side Hustle", "start_date": "2025-04-06"}`
3.  **BE logic:**
      * Validates the user token and start date.
      * **SQL Write:** Inserts a new row into the `businesses` table, linked to the `user_id`.
      * **Initializes Quarters:** Calculates the 4 fiscal quarters for the tax year starting 2025-04-06.
      * **NoSQL Writes:** Creates 4 initial documents in the `quarterly_updates` collection (one for each quarter) with `status: DRAFT`, and income and expenses set to `0.00`.
      * **Returns:** HTTP 201 CREATED with the new business id.
      * **Response Spec:** `{"business_id": 101, "name": "My Side Hustle"}`
4.  FE displays: Redirects to the `/dashboard` page.
5.  User is happy.

**Cucumber Scenario (B1):**

```gherkin
Scenario: Successful business registration and quarter initialization
Given the user is authenticated as user "42" and is on the "/setup" page
When the user enters "The Tech Emporium" into the Business Name field
And the user selects "2025-04-06" as the Accounting Start Date
And the user clicks the "Save Business Details" button
Then the application makes a POST request to "/api/business"
And the application receives a 201 status code with a business ID
And the backend has created 4 initial documents in the 'quarterly_updates' NoSQL collection
And the user is redirected to the "/dashboard" page
```

