using Kommand;
using Kommand.Sample.Commands;
using Kommand.Sample.Infrastructure;

namespace Kommand.Sample.Validators;

/// <summary>
/// Validator for CreateUserCommand.
/// Demonstrates:
/// - Async validation with database checks
/// - Multiple validation rules
/// - Injecting scoped dependencies (repository) into validators
/// </summary>
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Validate email is not empty
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("Email", "Email is required"));
        }
        // Validate email format (basic check)
        else if (!request.Email.Contains('@'))
        {
            errors.Add(new ValidationError("Email", "Email must be a valid email address"));
        }
        // ASYNC CHECK: Validate email is unique (demonstrates async validation with DB access)
        else if (await _repository.EmailExistsAsync(request.Email, cancellationToken))
        {
            errors.Add(new ValidationError("Email", $"Email '{request.Email}' is already registered"));
        }

        // Validate name is not empty
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError("Name", "Name is required"));
        }
        // Validate name length
        else if (request.Name.Length < 2)
        {
            errors.Add(new ValidationError("Name", "Name must be at least 2 characters long"));
        }
        else if (request.Name.Length > 100)
        {
            errors.Add(new ValidationError("Name", "Name must not exceed 100 characters"));
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
