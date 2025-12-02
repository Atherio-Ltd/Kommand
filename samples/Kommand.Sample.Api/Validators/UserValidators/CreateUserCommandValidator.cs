using Kommand;
using Kommand.Sample.Api.Commands.UserCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Validators.UserValidators;

/// <summary>
/// Validator for CreateUserCommand.
/// Demonstrates:
/// - Async validation with database checks (email uniqueness)
/// - Multiple validation rules
/// - Collecting all errors (not fail-fast)
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
        // Validate email format
        else if (!request.Email.Contains('@') || !request.Email.Contains('.'))
        {
            errors.Add(new ValidationError("Email", "Email must be a valid email address"));
        }
        // ASYNC: Validate email is unique (demonstrates async validation with DB access)
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

        // Validate phone number format if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            // Basic phone validation - allow digits, spaces, dashes, parentheses, plus
            if (!request.PhoneNumber.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '(' || c == ')' || c == '+'))
            {
                errors.Add(new ValidationError("PhoneNumber", "Phone number contains invalid characters"));
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
