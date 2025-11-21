namespace Kommand.Tests.Unit;

using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Unit tests for the validation system components.
/// Tests ValidationResult, ValidationError, ValidationException, and ValidationInterceptor behavior.
/// </summary>
public class ValidationTests
{
    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Success_ShouldHaveIsValidTrue()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Failure_WithSingleError_ShouldHaveIsValidFalse()
    {
        // Arrange
        var error = new ValidationError("Email", "Email is required");

        // Act
        var result = ValidationResult.Failure(error);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Email", result.Errors[0].PropertyName);
        Assert.Equal("Email is required", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ValidationResult_Failure_WithMultipleErrors_ShouldContainAllErrors()
    {
        // Arrange
        var error1 = new ValidationError("Email", "Email is required");
        var error2 = new ValidationError("Name", "Name is required");

        // Act
        var result = ValidationResult.Failure(error1, error2);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Email", result.Errors[0].PropertyName);
        Assert.Equal("Name", result.Errors[1].PropertyName);
    }

    [Fact]
    public void ValidationResult_Failure_WithNullErrors_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationResult.Failure((ValidationError[])null!));
    }

    [Fact]
    public void ValidationResult_Failure_WithEmptyErrors_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidationResult.Failure(Array.Empty<ValidationError>()));
    }

    [Fact]
    public void ValidationResult_Failure_WithIEnumerable_ShouldWork()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Email", "Email is required"),
            new ValidationError("Name", "Name is required")
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    #endregion

    #region ValidationError Tests

    [Fact]
    public void ValidationError_WithoutErrorCode_ShouldHaveNullErrorCode()
    {
        // Act
        var error = new ValidationError("Email", "Email is required");

        // Assert
        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Email is required", error.ErrorMessage);
        Assert.Null(error.ErrorCode);
    }

    [Fact]
    public void ValidationError_WithErrorCode_ShouldStoreErrorCode()
    {
        // Act
        var error = new ValidationError("Email", "Email is required", "REQUIRED");

        // Assert
        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("Email is required", error.ErrorMessage);
        Assert.Equal("REQUIRED", error.ErrorCode);
    }

    #endregion

    #region ValidationException Tests

    [Fact]
    public void ValidationException_WithErrors_ShouldStoreErrors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Email", "Email is required"),
            new ValidationError("Name", "Name is required")
        };

        // Act
        var exception = new ValidationException(errors.AsReadOnly());

        // Assert
        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains("2 error(s)", exception.Message);
    }

    [Fact]
    public void ValidationException_WithParamsArray_ShouldWork()
    {
        // Arrange & Act
        var exception = new ValidationException(
            new ValidationError("Email", "Email is required"),
            new ValidationError("Name", "Name is required"));

        // Assert
        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void ValidationException_WithNullErrors_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationException((IReadOnlyList<ValidationError>)null!));
    }

    [Fact]
    public void ValidationException_WithEmptyErrors_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ValidationException(Array.Empty<ValidationError>().AsReadOnly()));
    }

    [Fact]
    public void ValidationException_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new ValidationError("Email", "Email is required")
        };

        // Act
        var exception = new ValidationException("Custom validation message", errors.AsReadOnly());

        // Assert
        Assert.Equal("Custom validation message", exception.Message);
        Assert.Single(exception.Errors);
    }

    #endregion

    #region ValidationInterceptor Tests

    [Fact]
    public async Task ValidationInterceptor_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            Enumerable.Empty<IValidator<TestValidationCommand>>(),
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act
        var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task ValidationInterceptor_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var validator = new TestValidationCommandValidator(isValid: true);
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            new[] { validator },
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act
        var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task ValidationInterceptor_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new TestValidationCommandValidator(isValid: false);
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            new[] { validator },
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

        Assert.False(nextCalled); // Handler should NOT be called
        Assert.Single(exception.Errors);
        Assert.Equal("Value", exception.Errors[0].PropertyName);
        Assert.Equal("Test validation error", exception.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidationInterceptor_WithMultipleValidators_ShouldCollectAllErrors()
    {
        // Arrange
        var validator1 = new TestValidationCommandValidator(isValid: false, errorMessage: "Error 1");
        var validator2 = new TestValidationCommandValidator(isValid: false, errorMessage: "Error 2");
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            new[] { validator1, validator2 },
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal("Error 1", exception.Errors[0].ErrorMessage);
        Assert.Equal("Error 2", exception.Errors[1].ErrorMessage);
    }

    [Fact]
    public async Task ValidationInterceptor_WithMultipleValidators_OneValidOneFailing_ShouldCollectOnlyFailingErrors()
    {
        // Arrange
        var validator1 = new TestValidationCommandValidator(isValid: true);
        var validator2 = new TestValidationCommandValidator(isValid: false, errorMessage: "Error from validator 2");
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            new[] { validator1, validator2 },
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

        Assert.Single(exception.Errors);
        Assert.Equal("Error from validator 2", exception.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidationInterceptor_WithValidatorThatReturnsMultipleErrors_ShouldCollectAll()
    {
        // Arrange
        var validator = new MultiErrorValidator();
        var interceptor = new ValidationInterceptor<TestValidationCommand, string>(
            new[] { validator },
            NullLogger<ValidationInterceptor<TestValidationCommand, string>>.Instance);

        var command = new TestValidationCommand("test");
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Equal("Email", exception.Errors[0].PropertyName);
        Assert.Equal("Name", exception.Errors[1].PropertyName);
        Assert.Equal("Age", exception.Errors[2].PropertyName);
    }

    #endregion

    #region Test Helpers

    // Test command for validation tests
    public record TestValidationCommand(string Value) : ICommand<string>;

    // Simple validator for testing
    public class TestValidationCommandValidator : IValidator<TestValidationCommand>
    {
        private readonly bool _isValid;
        private readonly string _errorMessage;

        public TestValidationCommandValidator(bool isValid, string errorMessage = "Test validation error")
        {
            _isValid = isValid;
            _errorMessage = errorMessage;
        }

        public Task<ValidationResult> ValidateAsync(TestValidationCommand instance, CancellationToken cancellationToken)
        {
            if (_isValid)
            {
                return Task.FromResult(ValidationResult.Success());
            }

            return Task.FromResult(ValidationResult.Failure(
                new ValidationError("Value", _errorMessage)));
        }
    }

    // Validator that returns multiple errors
    public class MultiErrorValidator : IValidator<TestValidationCommand>
    {
        public Task<ValidationResult> ValidateAsync(TestValidationCommand instance, CancellationToken cancellationToken)
        {
            var errors = new[]
            {
                new ValidationError("Email", "Email is required"),
                new ValidationError("Name", "Name is required"),
                new ValidationError("Age", "Age must be 18 or older")
            };

            return Task.FromResult(ValidationResult.Failure(errors));
        }
    }

    #endregion
}
