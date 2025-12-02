# Kommand Sample API

A comprehensive Minimal API sample demonstrating all Kommand library features with real HTTP endpoints you can test via Postman, curl, or the Scalar API Reference UI.

## Features Demonstrated

This sample showcases all Kommand capabilities in a realistic API context:

| Feature | Example | Description |
|---------|---------|-------------|
| **Commands with Result** | `POST /api/users` | Creates a user and returns the created entity |
| **Void Commands (Unit)** | `PUT /api/users/{id}` | Updates a user without returning a value |
| **Queries (Single)** | `GET /api/users/{id}` | Returns a single user or null |
| **Queries (Collection)** | `GET /api/users` | Returns a paginated list of users |
| **Async Validation** | `POST /api/users` | Validates email uniqueness against database |
| **Error Collection** | `POST /api/users` | Collects all validation errors (not fail-fast) |
| **Notifications** | `POST /api/users` | Publishes domain events (email + audit handlers) |
| **Custom Interceptors** | All endpoints | Logs request/response with timing |
| **OpenTelemetry** | All endpoints | Distributed tracing and metrics |

## Running the Sample

```bash
cd samples/Kommand.Sample.Api
dotnet run
```

The API will start at `http://localhost:5000` (or the next available port).

## Testing with Scalar API Reference

1. Run the application
2. Open your browser to `http://localhost:5000/scalar/v1`
3. The Scalar API Reference UI will load with all endpoints documented
4. Try the endpoints directly from the browser using the interactive interface

Alternatively, access the raw OpenAPI document at `http://localhost:5000/openapi/v1.json`

## API Endpoints

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List all users (paginated) |
| GET | `/api/users/{id}` | Get user by ID |
| GET | `/api/users/by-email/{email}` | Get user by email |
| POST | `/api/users` | Create a new user |
| PUT | `/api/users/{id}` | Update an existing user |
| DELETE | `/api/users/{id}` | Deactivate a user (soft delete) |

### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products (paginated, with filters) |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/by-sku/{sku}` | Get product by SKU |
| POST | `/api/products` | Create a new product |
| PUT | `/api/products/{id}` | Update an existing product |

## Example Requests

### Create a User

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"email": "alice@example.com", "name": "Alice Johnson"}'
```

### Try Validation (Duplicate Email)

```bash
# Run the same request again - will fail with validation error
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"email": "alice@example.com", "name": "Alice Smith"}'
```

Response:
```json
{
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Email": ["Email 'alice@example.com' is already registered"]
  }
}
```

### List Users with Pagination

```bash
curl "http://localhost:5000/api/users?page=1&pageSize=10&activeOnly=true"
```

### Create a Product

```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Widget Pro", "sku": "WGT-PRO-001", "price": 29.99, "stockQuantity": 100}'
```

## Observing Kommand in Action

### Console Logs

Watch the console output to see:
- Custom interceptor logging (request/response with timing)
- Notification handlers executing (email, audit)
- OpenTelemetry trace spans

### OpenTelemetry

The sample is configured to export traces and metrics to the console. In production, you would configure an exporter like:
- Jaeger
- Zipkin
- Azure Application Insights
- AWS X-Ray
- Datadog
- New Relic

## Project Structure

```
Kommand.Sample.Api/
├── Commands/           # Command definitions (CreateUserCommand, etc.)
├── Queries/            # Query definitions (GetUserByIdQuery, etc.)
├── Handlers/           # Command, Query, and Notification handlers
├── Validators/         # Async validators with DB checks
├── Notifications/      # Domain event definitions
├── Interceptors/       # Custom logging interceptor
├── Infrastructure/     # Repository interfaces and in-memory implementations
├── Models/             # Domain models (User, Product)
├── DTOs/               # Request/Response DTOs
├── Endpoints/          # Feature-specific endpoint definitions
│   ├── UserEndpoints.cs
│   └── ProductEndpoints.cs
├── Middleware/         # Custom middleware
│   └── ExceptionHandlerExtensions.cs
└── Program.cs          # Clean application setup (~60 lines)
```

## Key Code Highlights

### Kommand Configuration (Program.cs)

```csharp
builder.Services.AddKommand(config =>
{
    // Auto-discover handlers, validators, notification handlers
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Add custom interceptor
    config.AddInterceptor(typeof(LoggingInterceptor<,>));

    // Enable validation
    config.WithValidation();
});
```

### Mapping Endpoints (Program.cs)

```csharp
// Endpoints are organized in feature-specific files
app.MapUserEndpoints();
app.MapProductEndpoints();
```

### Endpoint Definition Pattern (UserEndpoints.cs)

```csharp
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .Produces<UserResponse>(StatusCodes.Status201Created);

        return group;
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Email, request.Name, request.PhoneNumber);
        var user = await mediator.SendAsync(command, ct);
        return Results.Created($"/api/users/{user.Id}", user);
    }
}
```

### Async Validation with Database Check

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public async Task<ValidationResult> ValidateAsync(CreateUserCommand request, CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        // Async database check
        if (await _repository.EmailExistsAsync(request.Email, ct))
        {
            errors.Add(new ValidationError("Email", "Email already registered"));
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }
}
```

## Requirements

- .NET 10 SDK or later
- Kommand NuGet package (`Kommand 1.0.0-alpha.1`)
