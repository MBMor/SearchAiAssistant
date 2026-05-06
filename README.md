# Search & AI Assistant

Local-first ASP.NET Core Web API project demonstrating Clean Architecture, PostgreSQL persistence, OpenSearch full-text search, JWT authentication, indexing, and a retrieval-based AI assistant over company / HR knowledge-base data.

The project runs locally and does not require AWS, Azure, GCP, or any paid cloud service.

---

## Features

- JWT authentication
- Role-based authorization: `Admin` / `User`
- Employee management
- Document / knowledge-base management
- PostgreSQL as the source of truth
- OpenSearch full-text search
- Automatic indexing after create/update/delete
- Manual index rebuild
- Local mock/rule-based AI assistant
- Swagger / OpenAPI
- Health checks
- Global exception handling
- Request logging
- Unit tests
- Integration tests with Testcontainers

---

## Architecture

The solution follows a Clean Architecture / layered structure.

```text
src/
├── SearchAiAssistant.Api
├── SearchAiAssistant.Application
├── SearchAiAssistant.Domain
└── SearchAiAssistant.Infrastructure

tests/
├── SearchAiAssistant.Tests.Unit
└── SearchAiAssistant.Tests.Integration


Layers

Domain
- entities
- enums
- domain rules
- no external dependencies

Application
- use cases
- DTOs
- interfaces
- orchestration
- depends only on Domain

Infrastructure
- EF Core
- PostgreSQL
- OpenSearch
- JWT generation
- password hashing
- local AI assistant implementation

Api
- controllers
- middleware
- authentication setup
- Swagger
- dependency injection composition

Dependency direction:
Api -> Application -> Domain
Api -> Infrastructure -> Application / Domain

The API layer does not directly use EF Core for business logic.


Technology Stack
.NET 10
ASP.NET Core Web API
PostgreSQL
Entity Framework Core
OpenSearch
JWT Bearer authentication
Swagger / OpenAPI
Docker Compose
xUnit
Testcontainers
FluentAssertions


Used Technologies & Concepts
Clean Architecture
PostgreSQL source of truth
OpenSearch indexing
Full-text search
Relevance scoring
JWT authentication
Role-based authorization
Retrieval-based AI assistant flow
Mock AI provider
Health checks
Problem Details
Global exception handling
Unit tests
Integration tests
Docker Compose
Testcontainers


Domain

User
  Id
  Email
  PasswordHash
  Role
  CreatedAt

Employee
  Id
  FirstName
  LastName
  Email
  Department
  JobTitle
  Skills
  Location
  CreatedAt
  UpdatedAt

Document
  Id
  Title
  Content
  Category
  Tags
  CreatedAt
  UpdatedAt

Search index document
OpenSearch uses a separate index model for employees and documents.
Supported source types:
  employee
  document


Local Setup

Clone repository:
git clone https://github.com/MBMor/SearchAiAssistant.git
cd SearchAiAssistant

Restore and build:
dotnet restore
dotnet build

Start local infrastructure:
docker compose up -d

Apply database migrations:
dotnet ef database update --project src/SearchAiAssistant.Infrastructure --startup-project src/SearchAiAssistant.Api

Run API:
dotnet run --project src/SearchAiAssistant.Api

Open Swagger:
http://localhost:5080/swagger

Health endpoint:
http://localhost:5080/health


Docker Compose

Docker Compose starts:
PostgreSQL
OpenSearch

Useful commands:
docker compose up -d
docker compose ps
docker compose logs postgres
docker compose logs opensearch
docker compose down

Reset local data:
docker compose down -v

Default local services:
PostgreSQL: localhost:5432
OpenSearch: http://localhost:9200


Configuration

Main configuration:
src/SearchAiAssistant.Api/appsettings.json

Important sections:

{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=search_ai_assistant;Username=search_ai_assistant;Password=temporaryPassword"
  },
  "Jwt": {
    "Issuer": "SearchAiAssistant",
    "Audience": "SearchAiAssistant.Api",
    "SigningKey": "CHANGE_ME_IN_REAL_ENVIRONMENT",
    "AccessTokenExpirationMinutes": 60
  },
  "OpenSearch": {
    "Uri": "http://localhost:9200",
    "IndexName": "search-ai-assistant",
    "RequestTimeoutSeconds": 30
  },
  "AiAssistant": {
    "Provider": "Mock",
    "MaxRetrievedSources": 5,
    "MinimumScore": 0.1
  }
}

For real environments, override secrets with environment variables:
$env:Jwt__SigningKey = "your-long-secure-signing-key"
$env:ConnectionStrings__Postgres = "your-postgres-connection-string"


API Endpoints
Auth
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
Employees
GET    /api/employees
GET    /api/employees/{id}
POST   /api/employees
PUT    /api/employees/{id}
DELETE /api/employees/{id}
Documents
GET    /api/documents
GET    /api/documents/{id}
POST   /api/documents
PUT    /api/documents/{id}
DELETE /api/documents/{id}
Search
GET /api/search
GET /api/search/employees
GET /api/search/documents
Assistant
POST /api/assistant/ask
Indexing
POST /api/index/rebuild
POST /api/index/documents/{id}
POST /api/index/employees/{id}
Health
GET /health


Authentication

Register admin:

{
  "email": "admin@example.com",
  "password": "Password123!",
  "role": "Admin"
}

Login:

{
  "email": "admin@example.com",
  "password": "Password123!"
}

Use the returned accessToken in Swagger:
Click Authorize
Paste only the raw JWT token
Do not include the Bearer prefix


Authorization
User
- read employees
- read documents
- search
- ask assistant

Admin
- everything User can do
- create/update/delete employees
- create/update/delete documents
- rebuild search index


Search Flow
Client
  -> SearchController
  -> ISearchService
  -> OpenSearchSearchService
  -> OpenSearch

Search supports:
query
sourceType
category
department
jobTitle
tag
page
pageSize
sort

Search returns relevance score and optional highlights.


Indexing Flow
PostgreSQL is the source of truth.

On create/update:
Save to PostgreSQL
  -> map to search document
  -> index into OpenSearch

On delete:
Delete from PostgreSQL
  -> remove from OpenSearch

Manual rebuild:
POST /api/index/rebuild

This recreates the OpenSearch index from PostgreSQL.


AI Assistant Flow
The assistant is retrieval-based.

Question
  -> search relevant employees/documents
  -> build answer from retrieved sources
  -> return answer + sources

Current implementation:
LocalRuleBasedAiAssistant

It:
runs locally
does not use paid APIs
answers only from retrieved sources
returns sources
says there is not enough information if no relevant data is found

Example request:
{
  "question": "What benefits do employees have?",
  "maxSources": 5
}


Health Checks
GET /health

Checks:
API
PostgreSQL
OpenSearch


Testing

Run all tests:
dotnet test

Run unit tests:
dotnet test tests/SearchAiAssistant.Tests.Unit

Run integration tests:
dotnet test tests/SearchAiAssistant.Tests.Integration --logger "console;verbosity=detailed"

Integration tests use Testcontainers.
Docker Desktop must be running, but you do not need to start docker compose manually.


Design Decisions

PostgreSQL source of truth
OpenSearch is a derived search index and can be rebuilt.

One OpenSearch index
Employees and documents are stored in one search index to simplify global search and assistant retrieval.

Local AI first
The first assistant implementation is local and deterministic. No cloud or paid provider is required.

Simple services over MediatR
The project uses explicit Application services instead of MediatR to keep the code easier to read and explain.

Synchronous indexing
Indexing happens immediately after create/update/delete.


Troubleshooting
PostgreSQL container fails

Reset volumes:

docker compose down -v
docker compose up -d postgres
Search returns no results

Try rebuilding the index:
POST /api/index/rebuild

Also verify OpenSearch is running:
Invoke-RestMethod http://localhost:9200/_cluster/health


Future Improvements
refresh tokens
seed initial admin user
outbox pattern for reliable indexing
background worker for indexing
OpenAI-compatible assistant provider
semantic search / embeddings
OpenSearch Dashboards
CI pipeline
Dockerfile for API
Serilog structured logging
API versioning
rate limiting
more granular permissions


Suggested Demo Flow

Start infrastructure:
docker compose up -d

Apply migrations:
dotnet ef database update --project src/SearchAiAssistant.Infrastructure --startup-project src/SearchAiAssistant.Api

Run API:
dotnet run --project src/SearchAiAssistant.Api

Open Swagger:
http://localhost:5080/swagger
Register admin and authorize.
Create an employee.
Create a document.

Search:
GET /api/search?query=benefits

Ask assistant:
{
  "question": "What benefits do employees have?",
  "maxSources": 5
}
Rebuild index:
POST /api/index/rebuild


```text
