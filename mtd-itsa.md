# Project Specification: Full-Stack MTD-ITSA Compliance Portal Starter

## I. Project Overview and Goal

The primary goal is to create a robust, full-stack boilerplate for a **Making Tax Digital for Income Tax Self Assessment (MTD-ITSA) Compliance Portal**. The starter kit must include essential, pre-built cross-cutting concerns to enable rapid development of the core business logic.

**Key Deliverables:**
1.  Full-stack project structure using the official Microsoft template.
2.  Secure User Authentication (Register/Login).
3.  Admin Dashboard structure and navigation ready for integration.

## II. Technical Stack Requirements

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Backend (BE)** | **ASP.NET Core Web API (C#)** | Host the application, handle business logic, and manage data access. |
| **Frontend (FE)** | **Angular (TypeScript)** | Single Page Application (SPA) for the user interface. |
| **UI/Styling** | **Tailwind CSS** | Used for all styling (as demonstrated in the existing FE mockup). |
| **Architecture**| Official `.NET/Angular` Starter Template | Required structure for API proxy and hosting setup. |

## III. Required Functionality & API Contract (Immediate Focus: Authentication)

The immediate task is to establish the secure backend endpoints necessary for the provided frontend UI to manage user access.

### A. Authentication API Endpoints

The backend must implement a controller that handles the following two endpoints:

| Endpoint | HTTP Method | Purpose |
| :--- | :--- | :--- |
| `/api/auth/register` | `POST` | Create a new user account. |
| `/api/auth/login` | `POST` | Authenticate an existing user and return a JWT (mock or real). |

### B. Data Transfer Objects (DTOs)

The following C# DTOs are required to handle the request bodies for the authentication controller:

1.  **`RegisterRequest` DTO:**
    * `Email` (string, required)
    * `Password` (string, required)
    * `UserName` (string, required)

2.  **`LoginRequest` DTO:**
    * `Email` (string, required)
    * `Password` (string, required)

3.  **`AuthResponse` DTO:**
    * `UserId` (string, unique identifier)
    * `UserName` (string)
    * `Token` (string - should contain a mock or actual JWT)

## IV. Immediate Code Generation Task

The Code Assistant should generate the following C# files for the ASP.NET Core Web API project, using **mock or minimal ASP.NET Core Identity/JWT implementation** for the jumpstart, focusing on correct structure and DTO mapping.

1.  **Auth DTOs:** Classes for `RegisterRequest`, `LoginRequest`, and `AuthResponse`.
2.  **Auth Controller:** `AuthController.cs` implementing the `/api/auth/register` and `/api/auth/login` endpoints.

---

## V. Frontend Context (Reference UI/UX)

The generated backend code must service an Angular frontend that adheres to the structure provided in the original **React/Tailwind Admin Board Mockup** (filename: `AdminDashboard.jsx`). This means the `AuthResponse` must contain the necessary user information to display the user's name and email in the application header after a successful login.

The front-end structure (which the backend must support) includes:
* A dedicated **Login Screen** that posts to `/api/auth/login`.
* A **Sidebar Navigation** with links to Dashboard, Businesses, Users, and Settings.
* A **Header** displaying the authenticated user's name.
