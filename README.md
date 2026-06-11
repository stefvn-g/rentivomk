# RentivoMK — Vehicle Rental Management System

**Course:** Service Oriented Architectures  
**Faculty:** CST, SEE University    
**Semester:** 8th Semester (2025/2026)

---

## Table of Contents

- [About the Project](#about-the-project)
- [Team Members](#team-members)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Configuration](#configuration)
    - [Running the App](#running-the-app)
    - [Running the Tests](#running-the-tests)
- [API Overview](#api-overview)
- [User Roles & Permissions](#user-roles--permissions)
- [Architecture](#architecture)
- [Deployment](#deployment)
- [GitHub Repository](#github-repository)

---

## About the Project

RentivoMK is a web-based vehicle rental management system built as our final project for the Service Oriented Architectures course at SEE University. The idea behind it is simple — provide a clean, role-aware platform where customers can browse and reserve vehicles, workers can manage the day-to-day reservations, and administrators have full control over the entire system.

We built the backend as a RESTful ASP.NET Core Web API following a service-oriented approach, with clearly separated layers for data access, business logic, and API exposure. The system uses JWT authentication and role-based authorization to make sure every user only has access to what they're supposed to.

---

## Team Members

| Name | ID     |
|------|--------|
| Stefan Gavrovski | 130841 |
| Angel Nikoloski | 130847 |

---

## Features

- User registration and login with JWT authentication
- Role-based access control (Admin, Worker, Customer)
- Full CRUD operations for vehicles and users (Admin)
- Vehicle browsing and availability filtering (all authenticated users)
- Reservation lifecycle management — create, approve, reject, cancel, complete
- Date conflict detection to prevent double bookings
- Automatic vehicle status updates tied to reservation state changes
- Global exception handling middleware for consistent error responses
- Seed data on first run (admin account + sample vehicles and reservations)
- Unit tests for all core services
- Swagger UI for interactive API documentation

---

## Tech Stack

- **Framework:** ASP.NET Core Web API (.NET 10)
- **Database:** PostgreSQL via Npgsql + Entity Framework Core
- **Authentication:** JWT Bearer Tokens
- **Password Hashing:** BCrypt.Net
- **API Docs:** Swagger / OpenAPI
- **Testing:** xUnit + Moq
- **Deployment:** Azure
- **CI/CD:** GitHub Actions
- **Version Control:** Git + GitHub

---

## Project Structure

```
RentivoMK/
├── Controllers/          # API controllers (Auth, Users, Vehicles, Reservations)
├── Data/                 # DbContext and database seeder
├── DTOs/                 # Data Transfer Objects for requests and responses
├── Enums/                # Enumerations (UserRole, VehicleStatus, ReservationStatus)
├── Interfaces/           # Contracts for repositories and services
├── Middleware/           # Global exception handling middleware
├── Models/               # Entity models (User, Vehicle, Reservation)
├── Repositories/         # EF Core repository implementations
├── Services/             # Business logic layer
├── appsettings.json      # Application configuration
├── Program.cs            # App startup, DI registration, middleware pipeline
└── RentivoMK.csproj

RentivoMK.Tests/
├── AuthServiceTests.cs
├── ReservationServiceTests.cs
├── UserServiceTests.cs
├── VehicleServiceTests.cs
└── RentivoMK.Tests.csproj
```

---

## Getting Started

### Prerequisites

Make sure you have the following installed before running the project:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/)
- A PostgreSQL database created and ready (e.g. `rentivomk`)

### Configuration

Open `appsettings.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=rentivomk;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyHere",
    "Issuer": "RentivoMK",
    "Audience": "RentivoMK",
    "ExpiryInMinutes": 60
  },
  "AllowedOrigins": "http://localhost:5173"
}
```

> The `SecretKey` should be a long random string (at least 32 characters) for security.

### Running the App

```bash
# Clone the repository
git clone https://github.com/stefangavrovski/rentivomk.git
cd rentivomk

# Navigate to the API project
cd RentivoMK

# Restore dependencies
dotnet restore

# Apply database migrations and start the server
dotnet run
```

On first startup, the app will automatically apply any pending migrations and seed the database with:

- An admin account: `admin@rentivomk.com` / `Admin@123`
- A sample customer: `jane.doe@example.com` / `Customer@123`
- 5 sample vehicles
- 2 sample reservations

Once running, the Swagger UI is available at:
```
http://localhost:8080/swagger
```

### Running the Tests

```bash
# Navigate to the test project
cd RentivoMK.Tests

# Run all tests
dotnet test
```

All tests use mocked repositories, so no database connection is needed to run them.

---

## API Overview

All endpoints (except auth) require a valid JWT token sent as a Bearer token in the `Authorization` header.

### Auth — `/api/auth`

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| POST | `/api/auth/register` | Register a new customer account | Public |
| POST | `/api/auth/login` | Login and receive a JWT token | Public |

### Users — `/api/users`

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/users` | Get all users | Admin |
| GET | `/api/users/{id}` | Get a user by ID | Admin, Worker |
| PUT | `/api/users/{id}` | Update a user | Admin |
| DELETE | `/api/users/{id}` | Delete a user | Admin |

### Vehicles — `/api/vehicles`

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/vehicles` | Get all vehicles | All authenticated |
| GET | `/api/vehicles/available` | Get only available vehicles | All authenticated |
| GET | `/api/vehicles/{id}` | Get a vehicle by ID | All authenticated |
| POST | `/api/vehicles` | Add a new vehicle | Admin |
| PUT | `/api/vehicles/{id}` | Update a vehicle | Admin |
| DELETE | `/api/vehicles/{id}` | Delete a vehicle | Admin |

### Reservations — `/api/reservations`

| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/reservations` | Get all reservations | Admin, Worker |
| GET | `/api/reservations/my` | Get current user's reservations | Customer |
| GET | `/api/reservations/{id}` | Get a reservation by ID | Admin, Worker, owning Customer |
| POST | `/api/reservations` | Create a new reservation | Customer |
| PUT | `/api/reservations/{id}/approve` | Approve a pending reservation | Admin, Worker |
| PUT | `/api/reservations/{id}/reject` | Reject a pending reservation | Admin, Worker |
| PUT | `/api/reservations/{id}/complete` | Mark a reservation as completed | Admin, Worker |
| PUT | `/api/reservations/{id}/cancel` | Cancel a reservation | Admin, owning Customer |

---

## User Roles & Permissions

We implemented three roles with clearly separated permissions:

**Admin** — has full control over the system. Can manage all users, vehicles, and reservations, including actions like approving, rejecting, cancelling, and completing reservations. Can also delete vehicles (as long as no active reservations exist) and assign or change user roles.

**Worker** — focused on day-to-day reservation management. Can view all reservations and users, and can approve, reject, or complete reservations, but cannot manage users or delete vehicles.

**Customer** — the end user. Can browse vehicles, create reservations for available vehicles, view their own reservations, and cancel them if needed. Cannot access other users' data or perform any administrative actions.

---

## Architecture

We followed a clean layered architecture to keep concerns separated and the code maintainable:

- **Controllers** handle incoming HTTP requests, validate routing and authorization, and delegate work to services.
- **Services** contain all the business logic — things like date conflict checks, status transitions, price calculations, and authorization rules.
- **Repositories** are the only layer that talks to the database. They implement generic and entity-specific interfaces, making the services easy to unit test with mocks.
- **DTOs** keep the API contract clean and decouple what gets sent over the wire from the internal database models.
- **Middleware** provides centralized exception handling, mapping known exception types (like `KeyNotFoundException` or `UnauthorizedAccessException`) to the right HTTP status codes without repeating that logic everywhere.

This separation means we could write all of our unit tests using mocked repositories — no database needed — which keeps the tests fast and reliable.

---

## Deployment

The application is deployed on **Azure** using **GitHub Actions** for CI/CD. Every push to the main branch triggers the pipeline, which builds the project, runs the tests, and deploys to Azure automatically.

The `PORT` environment variable is used to configure the server's listening port at runtime (defaults to `8080`), and the database connection string and JWT secret are managed via Azure environment configuration (not stored in source code).

---

## License

This project was created for educational purposes as part of university coursework.
