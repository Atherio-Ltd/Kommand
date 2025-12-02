using Kommand.Abstractions;
using Kommand.Sample.Api.Commands;
using Kommand.Sample.Api.DTOs;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries;

namespace Kommand.Sample.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users");

        group.MapGet("/", ListUsers)
            .WithName("ListUsers")
            .WithSummary("List all users")
            .WithDescription("Returns a paginated list of users. Demonstrates a Query returning a collection.")
            .Produces<object>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetUserById)
            .WithName("GetUserById")
            .WithSummary("Get user by ID")
            .WithDescription("Returns a single user by ID. Demonstrates a Query returning a single object (or null).")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/by-email/{email}", GetUserByEmail)
            .WithName("GetUserByEmail")
            .WithSummary("Get user by email")
            .WithDescription("Returns a single user by email address.")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .WithDescription("""
                Creates a new user. Demonstrates:
                - Command with result (returns created user)
                - Async validation with database check (email uniqueness)
                - Publishing notifications after success (welcome email + audit log)

                Try creating with duplicate email to see validation in action.
                """)
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser")
            .WithSummary("Update an existing user")
            .WithDescription("Updates a user. Demonstrates a void Command returning Unit (no value).")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeactivateUser)
            .WithName("DeactivateUser")
            .WithSummary("Deactivate a user (soft delete)")
            .WithDescription("Deactivates a user. Demonstrates a void Command that publishes a notification.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListUsers(
        IMediator mediator,
        bool? activeOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ListUsersQuery(activeOnly, page, pageSize);
        var result = await mediator.QueryAsync(query, ct);

        return Results.Ok(new
        {
            result.Items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage
        });
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var user = await mediator.QueryAsync(query, ct);

        if (user == null)
        {
            return Results.NotFound(new ErrorResponse(
                Title: "Not Found",
                Status: 404,
                Detail: $"User with ID {id} not found"));
        }

        return Results.Ok(ToUserResponse(user));
    }

    private static async Task<IResult> GetUserByEmail(
        string email,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetUserByEmailQuery(email);
        var user = await mediator.QueryAsync(query, ct);

        if (user == null)
        {
            return Results.NotFound(new ErrorResponse(
                Title: "Not Found",
                Status: 404,
                Detail: $"User with email '{email}' not found"));
        }

        return Results.Ok(ToUserResponse(user));
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Email, request.Name, request.PhoneNumber);
        var user = await mediator.SendAsync(command, ct);

        return Results.Created($"/api/users/{user.Id}", ToUserResponse(user));
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new UpdateUserCommand(id, request.Name, request.PhoneNumber);
        await mediator.SendAsync(command, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateUser(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new DeactivateUserCommand(id);
        await mediator.SendAsync(command, ct);

        return Results.NoContent();
    }

    private static UserResponse ToUserResponse(User user) =>
        new(user.Id, user.Email, user.Name, user.PhoneNumber, user.CreatedAt, user.UpdatedAt, user.IsActive);
}
