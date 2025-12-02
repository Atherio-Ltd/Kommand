using Kommand;
using Kommand.Sample.Api.Commands.UserCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Validators.UserValidators;

/// <summary>
/// Validator for UpdateUserCommand.
/// </summary>
public class UpdateUserCommandValidator : IValidator<UpdateUserCommand>
{
    private readonly IUserRepository _repository;

    public UpdateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        UpdateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Validate user exists
        var user = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            errors.Add(new ValidationError("UserId", $"User with ID {request.UserId} not found"));
        }
        else if (!user.IsActive)
        {
            errors.Add(new ValidationError("UserId", "Cannot update a deactivated user"));
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError("Name", "Name is required"));
        }
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
