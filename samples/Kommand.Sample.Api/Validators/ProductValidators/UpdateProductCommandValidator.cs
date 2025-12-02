using Kommand;
using Kommand.Sample.Api.Commands.ProductCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Validators.ProductValidators;

/// <summary>
/// Validator for UpdateProductCommand.
/// </summary>
public class UpdateProductCommandValidator : IValidator<UpdateProductCommand>
{
    private readonly IProductRepository _repository;

    public UpdateProductCommandValidator(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        UpdateProductCommand request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Validate product exists
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            errors.Add(new ValidationError("ProductId", $"Product with ID {request.ProductId} not found"));
        }
        else if (!product.IsActive)
        {
            errors.Add(new ValidationError("ProductId", "Cannot update a deactivated product"));
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError("Name", "Product name is required"));
        }
        else if (request.Name.Length > 200)
        {
            errors.Add(new ValidationError("Name", "Product name must not exceed 200 characters"));
        }

        // Validate price
        if (request.Price < 0)
        {
            errors.Add(new ValidationError("Price", "Price cannot be negative"));
        }

        // Validate stock quantity
        if (request.StockQuantity < 0)
        {
            errors.Add(new ValidationError("StockQuantity", "Stock quantity cannot be negative"));
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
