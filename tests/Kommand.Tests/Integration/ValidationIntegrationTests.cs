namespace Kommand.Tests.Integration;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

/// <summary>
/// Integration tests for the validation system.
/// Tests end-to-end validation flow with DI container, mediator, and validators.
/// </summary>
public class ValidationIntegrationTests
{
    [Fact]
    public async Task SendAsync_WithValidationEnabled_ValidRequest_ShouldExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(
            new CreateUserCommand("john@example.com", "John Doe"),
            CancellationToken.None);

        // Assert
        Assert.Equal("User created: john@example.com", result);
    }

    [Fact]
    public async Task SendAsync_WithValidationEnabled_InvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.SendAsync(
                new CreateUserCommand("", ""), // Invalid: empty email and name
                CancellationToken.None));

        // Should have errors from the validator
        Assert.True(exception.Errors.Count >= 2); // At least email and name errors
        Assert.Contains(exception.Errors, e => e.PropertyName == "Email");
        Assert.Contains(exception.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task SendAsync_WithMultipleValidators_ShouldCollectAllErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Both validators should run and collect errors
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.SendAsync(
                new CreateUserCommand("invalid", "123"), // Invalid email format and name with numbers
                CancellationToken.None));

        // Should have errors from both validators
        Assert.True(exception.Errors.Count >= 2);
    }

    [Fact]
    public async Task SendAsync_WithoutValidationEnabled_InvalidRequest_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            // Note: NOT calling config.WithValidation()
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Should NOT throw because validation is not enabled
        var result = await mediator.SendAsync(
            new CreateUserCommand("", ""), // Invalid, but validation is disabled
            CancellationToken.None);

        // Assert - Handler should execute despite invalid data
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendAsync_WithValidationEnabled_NoValidatorsForCommand_ShouldExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - CommandWithoutValidator has no validators
        var result = await mediator.SendAsync(
            new CommandWithoutValidator("test"),
            CancellationToken.None);

        // Assert - Should execute without validation
        Assert.Equal("executed: test", result);
    }

    [Fact]
    public async Task SendAsync_WithAsyncValidator_ShouldSupportAsyncValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<FakeUserRepository>(); // Add fake repository
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Email "duplicate@example.com" exists in fake repository
        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.SendAsync(
                new CreateUserCommand("duplicate@example.com", "John"),
                CancellationToken.None));

        Assert.Contains(exception.Errors, e =>
            e.PropertyName == "Email" && e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task SendAsync_WithValidationError_ShouldNotExecuteHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Track if handler was called
        CreateUserCommandHandler.ResetExecutionCount();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.SendAsync(
                new CreateUserCommand("", ""), // Invalid
                CancellationToken.None));

        // Handler should NOT have been executed
        Assert.Equal(0, CreateUserCommandHandler.ExecutionCount);
    }

    [Fact]
    public async Task SendAsync_WithValidRequest_ShouldExecuteHandlerOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Critical));
        services.AddSingleton<FakeUserRepository>(); // Required by CreateUserAsyncValidator
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ValidationIntegrationTests).Assembly);
            config.WithValidation();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Track if handler was called
        CreateUserCommandHandler.ResetExecutionCount();

        // Act
        await mediator.SendAsync(
            new CreateUserCommand("valid@example.com", "John Doe"),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, CreateUserCommandHandler.ExecutionCount);
    }

    #region Test Commands, Handlers, and Validators

    // Command for user creation
    public record CreateUserCommand(string Email, string Name) : ICommand<string>;

    // Handler for user creation
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, string>
    {
        private static int _executionCount = 0;
        public static int ExecutionCount => _executionCount;
        public static void ResetExecutionCount() => _executionCount = 0;

        public Task<string> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);
            return Task.FromResult($"User created: {command.Email}");
        }
    }

    // Basic validator for CreateUserCommand
    public class CreateUserCommandValidator : IValidator<CreateUserCommand>
    {
        public Task<ValidationResult> ValidateAsync(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.Email))
            {
                errors.Add(new ValidationError("Email", "Email is required"));
            }
            else if (!command.Email.Contains("@"))
            {
                errors.Add(new ValidationError("Email", "Email must be valid"));
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                errors.Add(new ValidationError("Name", "Name is required"));
            }

            return Task.FromResult(errors.Any()
                ? ValidationResult.Failure(errors.ToArray())
                : ValidationResult.Success());
        }
    }

    // Business rules validator for CreateUserCommand (multiple validators for same command)
    public class CreateUserBusinessRulesValidator : IValidator<CreateUserCommand>
    {
        public Task<ValidationResult> ValidateAsync(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            // Business rule: Name cannot contain numbers
            if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Any(char.IsDigit))
            {
                errors.Add(new ValidationError("Name", "Name cannot contain numbers"));
            }

            return Task.FromResult(errors.Any()
                ? ValidationResult.Failure(errors.ToArray())
                : ValidationResult.Success());
        }
    }

    // Async validator that checks database
    public class CreateUserAsyncValidator : IValidator<CreateUserCommand>
    {
        private readonly FakeUserRepository _repository;

        public CreateUserAsyncValidator(FakeUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ValidationResult> ValidateAsync(CreateUserCommand command, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            // Async validation - check if email exists in database
            if (!string.IsNullOrWhiteSpace(command.Email) &&
                await _repository.EmailExistsAsync(command.Email, cancellationToken))
            {
                errors.Add(new ValidationError("Email", "Email already exists"));
            }

            return errors.Any()
                ? ValidationResult.Failure(errors.ToArray())
                : ValidationResult.Success();
        }
    }

    // Command without any validators (to test that validation doesn't break when no validators exist)
    public record CommandWithoutValidator(string Value) : ICommand<string>;

    public class CommandWithoutValidatorHandler : ICommandHandler<CommandWithoutValidator, string>
    {
        public Task<string> HandleAsync(CommandWithoutValidator command, CancellationToken cancellationToken)
        {
            return Task.FromResult($"executed: {command.Value}");
        }
    }

    // Fake repository for testing async validation
    public class FakeUserRepository
    {
        private readonly HashSet<string> _existingEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "duplicate@example.com",
            "existing@example.com"
        };

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(_existingEmails.Contains(email));
        }
    }

    #endregion
}
