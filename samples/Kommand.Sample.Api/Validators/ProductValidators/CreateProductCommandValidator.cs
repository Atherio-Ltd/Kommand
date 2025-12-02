using Kommand;
using Kommand.Sample.Api.Commands.ProductCommands;
using Kommand.Sample.Api.Infrastructure;

namespace Kommand.Sample.Api.Validators.ProductValidators;

/// <summary>
/// Validator for CreateProductCommand.
/// Demonstrates multiple business rule validations.
/// </summary>
public class CreateProductCommandValidator : IValidator<CreateProductCommand>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandValidator(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateProductCommand request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError("Name", "Product name is required"));
        }
        else if (request.Name.Length > 200)
        {
            errors.Add(new ValidationError("Name", "Product name must not exceed 200 characters"));
        }

        // Validate SKU
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            errors.Add(new ValidationError("Sku", "SKU is required"));
        }
        else if (request.Sku.Length > 50)
        {
            errors.Add(new ValidationError("Sku", "SKU must not exceed 50 characters"));
        }
        // ASYNC: Check SKU uniqueness
        else if (await _repository.SkuExistsAsync(request.Sku, cancellationToken))
        {
            errors.Add(new ValidationError("Sku", $"SKU '{request.Sku}' already exists"));
        }

        // Validate price
        if (request.Price < 0)
        {
            errors.Add(new ValidationError("Price", "Price cannot be negative"));
        }
        else if (request.Price > 1_000_000)
        {
            errors.Add(new ValidationError("Price", "Price cannot exceed 1,000,000"));
        }

        // Validate stock quantity
        if (request.StockQuantity < 0)
        {
            errors.Add(new ValidationError("StockQuantity", "Stock quantity cannot be negative"));
        }

        // Validate description length if provided
        if (request.Description?.Length > 2000)
        {
            errors.Add(new ValidationError("Description", "Description must not exceed 2000 characters"));
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
