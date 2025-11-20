# Kommand Implementation Task List - Part 2

## Continuation Information
**This document is a continuation of**: `KOMMAND_TASK_LIST.md`
**Previous phases completed in Part 1**:
- Phase 1: Core Foundation (15 tasks)
- Phase 2: Interceptor System (8 tasks, includes 3 tasks not fully detailed)

**This document contains**: Phases 3-6 (remaining implementation)

**Total tasks in Part 2**: 29 tasks

---

## Phase 3: OpenTelemetry Integration (6 tasks)

### Task 3.1: Implement ActivityInterceptor
**Status**: [ ] Not Started

**Objective**: Create interceptor for distributed tracing with OpenTelemetry (Architecture Doc Section 10, lines 1034-1070).

**Instructions**:
1. In `src/Kommand/Interceptors/`, create `ActivityInterceptor.cs`:
   ```csharp
   namespace Kommand;

   using System.Diagnostics;

   /// <summary>
   /// Built-in interceptor that creates OpenTelemetry Activities for distributed tracing.
   /// Zero overhead when OTEL is not configured (~10-50ns).
   /// </summary>
   public class ActivityInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       private static readonly ActivitySource ActivitySource = new("Kommand", "1.0.0");

       public async Task<TResponse> HandleAsync(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken)
       {
           var requestName = typeof(TRequest).Name;
           var requestType = request is ICommand ? "Command" : "Query";

           // Returns NULL if no OTEL configured - zero overhead!
           using var activity = ActivitySource.StartActivity(
               $"{requestType}.{requestName}",
               ActivityKind.Internal);

           // All null-safe - no performance impact if activity is null
           activity?.SetTag("kommand.request.type", requestType);
           activity?.SetTag("kommand.request.name", requestName);

           try
           {
               var response = await next();
               activity?.SetStatus(ActivityStatusCode.Ok);
               return response;
           }
           catch (Exception ex)
           {
               activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
               activity?.RecordException(ex);
               throw;
           }
       }
   }
   ```
2. Add comprehensive XML documentation explaining:
   - What Activities are (distributed tracing spans)
   - Zero overhead when OTEL not configured
   - How to view traces (requires OTEL configuration in app)
   - What tags are set automatically
3. Reference Architecture Doc Section 10, lines 1034-1070

**How it fits**: Automatically creates distributed tracing spans for every request without user configuration.

**Builds upon**:
- Task 1.8 (System.Diagnostics.DiagnosticSource package)
- Task 2.1 (IInterceptor interface)

**Future dependencies**:
- Task 3.3 (automatic registration)
- Task 3.5 (testing)

**Completion Checklist**:
- [ ] ActivityInterceptor class created in `Interceptors/` folder
- [ ] Static ActivitySource with name "Kommand" and version "1.0.0"
- [ ] Creates Activity with format: "{Command|Query}.{RequestName}"
- [ ] Uses ActivityKind.Internal
- [ ] Sets tags: `kommand.request.type` and `kommand.request.name`
- [ ] Uses `using` statement to dispose Activity
- [ ] Sets Activity status to Ok on success
- [ ] Sets Activity status to Error and records exception on failure
- [ ] All Activity operations are null-safe (using `?.`)
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 3.2: Implement MetricsInterceptor
**Status**: [ ] Not Started

**Objective**: Create interceptor for OpenTelemetry metrics (Architecture Doc Section 10, lines 1131-1135).

**Instructions**:
1. In `src/Kommand/Interceptors/`, create `MetricsInterceptor.cs`:
   ```csharp
   namespace Kommand;

   using System.Diagnostics;
   using System.Diagnostics.Metrics;

   /// <summary>
   /// Built-in interceptor that records OpenTelemetry metrics.
   /// Zero overhead when OTEL is not configured (~5-10ns).
   /// </summary>
   public class MetricsInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       private static readonly Meter Meter = new("Kommand", "1.0.0");

       private static readonly Histogram<double> RequestDuration =
           Meter.CreateHistogram<double>(
               "kommand.request.duration",
               unit: "ms",
               description: "Duration of request processing in milliseconds");

       private static readonly Counter<long> RequestCount =
           Meter.CreateCounter<long>(
               "kommand.request.count",
               description: "Total number of requests processed");

       public async Task<TResponse> HandleAsync(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken)
       {
           var requestName = typeof(TRequest).Name;
           var requestType = request is ICommand ? "Command" : "Query";
           var startTime = Stopwatch.GetTimestamp();

           try
           {
               var response = await next();

               // Record success metrics
               var elapsedMs = GetElapsedMilliseconds(startTime);
               RequestDuration.Record(elapsedMs,
                   new("request_name", requestName),
                   new("request_type", requestType),
                   new("status", "success"));
               RequestCount.Add(1,
                   new("request_name", requestName),
                   new("request_type", requestType),
                   new("status", "success"));

               return response;
           }
           catch (Exception)
           {
               // Record failure metrics
               var elapsedMs = GetElapsedMilliseconds(startTime);
               RequestDuration.Record(elapsedMs,
                   new("request_name", requestName),
                   new("request_type", requestType),
                   new("status", "failure"));
               RequestCount.Add(1,
                   new("request_name", requestName),
                   new("request_type", requestType),
                   new("status", "failure"));

               throw;
           }
       }

       private static double GetElapsedMilliseconds(long startTimestamp)
       {
           var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
           return elapsed * 1000.0 / Stopwatch.Frequency;
       }
   }
   ```
2. Add comprehensive XML documentation explaining:
   - What metrics are recorded
   - Dimensions (tags): request_name, request_type, status
   - Zero overhead when OTEL not configured
   - How to view metrics (requires OTEL configuration in app)
3. Reference Architecture Doc Section 10, lines 1131-1135

**How it fits**: Automatically records performance metrics for every request without user configuration.

**Builds upon**:
- Task 1.8 (System.Diagnostics.DiagnosticSource package)
- Task 2.1 (IInterceptor interface)

**Future dependencies**:
- Task 3.3 (automatic registration)
- Task 3.6 (testing)

**Completion Checklist**:
- [ ] MetricsInterceptor class created in `Interceptors/` folder
- [ ] Static Meter with name "Kommand" and version "1.0.0"
- [ ] Duration histogram created with name "kommand.request.duration"
- [ ] Duration histogram unit is "ms"
- [ ] Request counter created with name "kommand.request.count"
- [ ] Uses Stopwatch for accurate duration measurement
- [ ] Records metrics with tags: request_name, request_type, status
- [ ] Records both success and failure metrics
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 3.3: Implement Auto-Registration with IConfigureOptions
**Status**: [ ] Not Started

**Objective**: Create automatic OTEL registration using IConfigureOptions pattern (Architecture Doc Section 10, lines 1077-1093).

**Instructions**:
1. In `src/Kommand/Registration/`, create `KommandTracerOptions.cs`:
   ```csharp
   namespace Kommand;

   using Microsoft.Extensions.Options;
   using OpenTelemetry.Trace;

   /// <summary>
   /// Automatically configures OTEL tracing to subscribe to Kommand's ActivitySource.
   /// This is registered as IConfigureOptions and runs when OTEL is configured.
   /// </summary>
   internal sealed class KommandTracerOptions : IConfigureOptions<TracerProviderBuilder>
   {
       public void Configure(TracerProviderBuilder builder)
       {
           builder.AddSource("Kommand");
       }
   }
   ```
2. Create `KommandMeterOptions.cs` in same directory:
   ```csharp
   namespace Kommand;

   using Microsoft.Extensions.Options;
   using OpenTelemetry.Metrics;

   /// <summary>
   /// Automatically configures OTEL metrics to subscribe to Kommand's Meter.
   /// This is registered as IConfigureOptions and runs when OTEL is configured.
   /// </summary>
   internal sealed class KommandMeterOptions : IConfigureOptions<MeterProviderBuilder>
   {
       public void Configure(MeterProviderBuilder builder)
       {
           builder.AddMeter("Kommand");
       }
   }
   ```
3. Add NuGet package references to `Kommand.csproj`:
   ```xml
   <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
   <PackageReference Include="OpenTelemetry" Version="1.9.0" />
   ```
   Note: These are for build-time only; at runtime they're provided by the host app
   Note: OpenTelemetry 1.9.0 supports .NET 8 (independent versioning from .NET)
4. Update `ServiceCollectionExtensions.AddKommand()` (from Task 1.14) to register OTEL components:
   ```csharp
   // ALWAYS register OTEL interceptors (safe, zero overhead when OTEL not configured)
   services.TryAddSingleton(
       typeof(IInterceptor<,>),
       typeof(ActivityInterceptor<,>));
   services.TryAddSingleton(
       typeof(IInterceptor<,>),
       typeof(MetricsInterceptor<,>));

   // Auto-configure OTEL (deferred - runs when OTEL is configured)
   services.TryAddEnumerable(
       ServiceDescriptor.Singleton<IConfigureOptions<TracerProviderBuilder>,
           KommandTracerOptions>());
   services.TryAddEnumerable(
       ServiceDescriptor.Singleton<IConfigureOptions<MeterProviderBuilder>,
           KommandMeterOptions>());
   ```
5. Add XML documentation explaining the deferred configuration pattern

**How it fits**: OTEL interceptors are automatically registered and will auto-subscribe to OTEL when the user configures it.

**Builds upon**:
- Task 3.1 (ActivityInterceptor)
- Task 3.2 (MetricsInterceptor)
- Task 1.14 (ServiceCollectionExtensions)

**Future dependencies**:
- Task 3.4 (add opt-out configuration)
- Task 3.5 & 3.6 (testing)

**Completion Checklist**:
- [ ] KommandTracerOptions class created
- [ ] Implements IConfigureOptions<TracerProviderBuilder>
- [ ] Adds "Kommand" ActivitySource
- [ ] KommandMeterOptions class created
- [ ] Implements IConfigureOptions<MeterProviderBuilder>
- [ ] Adds "Kommand" Meter
- [ ] Both classes are internal sealed
- [ ] Microsoft.Extensions.Options package added
- [ ] OpenTelemetry package added
- [ ] ServiceCollectionExtensions updated with OTEL registration
- [ ] Uses TryAddSingleton for interceptors
- [ ] Uses TryAddEnumerable for IConfigureOptions
- [ ] XML documentation complete
- [ ] Project builds successfully

---

### Task 3.4: Add DisableOpenTelemetry Configuration Option
**Status**: [ ] Not Started

**Objective**: Allow users to opt-out of OTEL if needed (Architecture Doc Section 10, lines 1145-1152).

**Instructions**:
1. In `src/Kommand/Registration/KommandConfiguration.cs` (from Task 1.12), add property:
   ```csharp
   /// <summary>
   /// Disables automatic OpenTelemetry integration (rare use case).
   /// Default: false (OTEL is enabled).
   /// </summary>
   public bool DisableOpenTelemetry { get; set; } = false;
   ```
2. Update `ServiceCollectionExtensions.AddKommand()` to respect this flag:
   ```csharp
   // OTEL interceptors (unless explicitly disabled)
   if (!config.DisableOpenTelemetry)
   {
       services.TryAddSingleton(
           typeof(IInterceptor<,>),
           typeof(ActivityInterceptor<,>));
       services.TryAddSingleton(
           typeof(IInterceptor<,>),
           typeof(MetricsInterceptor<,>));

       services.TryAddEnumerable(
           ServiceDescriptor.Singleton<IConfigureOptions<TracerProviderBuilder>,
               KommandTracerOptions>());
       services.TryAddEnumerable(
           ServiceDescriptor.Singleton<IConfigureOptions<MeterProviderBuilder>,
               KommandMeterOptions>());
   }
   ```
3. Add XML documentation explaining when to use this (rare edge cases only)

**How it fits**: Provides escape hatch for users who don't want OTEL for any reason.

**Builds upon**: Task 3.3 (OTEL registration)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] DisableOpenTelemetry property added to KommandConfiguration
- [ ] Default value is false (OTEL enabled by default)
- [ ] ServiceCollectionExtensions respects the flag
- [ ] OTEL components NOT registered when flag is true
- [ ] XML documentation explains use case
- [ ] Project builds successfully

---

### Task 3.5: Write Unit Tests for ActivityInterceptor
**Status**: [ ] Not Started

**Objective**: Test Activity creation and tracing behavior (Architecture Doc Section 12).

**Instructions**:
1. Create test file `Unit/ActivityInterceptorTests.cs` in `Kommand.Tests` project:
   ```csharp
   namespace Kommand.Tests.Unit;

   using System.Diagnostics;
   using FluentAssertions;

   public class ActivityInterceptorTests
   {
       [Fact]
       public async Task HandleAsync_CreatesActivity_WithCorrectName()
       {
           // Arrange
           var listener = new ActivityListener
           {
               ShouldListenTo = source => source.Name == "Kommand",
               Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
           };
           ActivitySource.AddActivityListener(listener);

           var interceptor = new ActivityInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           var nextCalled = false;
           RequestHandlerDelegate<string> next = () =>
           {
               nextCalled = true;
               return Task.FromResult("result");
           };

           // Act
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           nextCalled.Should().BeTrue();
           result.Should().Be("result");

           // Activity would have been created with name "Command.TestCommand"
           // Note: Testing Activity behavior requires ActivityListener setup
       }

       [Fact]
       public async Task HandleAsync_WithoutOTEL_HasZeroOverhead()
       {
           // Arrange
           var interceptor = new ActivityInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => Task.FromResult("result");

           // Act - no ActivityListener attached, Activity will be null
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           result.Should().Be("result");
           // This test documents that Activity creation is null-safe
       }

       [Fact]
       public async Task HandleAsync_OnException_SetsErrorStatus()
       {
           // Arrange
           Activity? capturedActivity = null;
           var listener = new ActivityListener
           {
               ShouldListenTo = source => source.Name == "Kommand",
               Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
               ActivityStopped = activity => capturedActivity = activity
           };
           ActivitySource.AddActivityListener(listener);

           var interceptor = new ActivityInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test error");

           // Act & Assert
           await Assert.ThrowsAsync<InvalidOperationException>(
               async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

           // Cleanup
           listener.Dispose();
       }
   }

   // Test fixtures
   public record TestCommand(string Value) : ICommand<string>;
   ```
2. Add more tests as needed to verify tag setting behavior
3. Reference .NET documentation for ActivityListener testing patterns

**How it fits**: Ensures distributed tracing works correctly.

**Builds upon**:
- Task 3.1 (ActivityInterceptor)
- Task 1.15 (test project already created)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] `Unit/ActivityInterceptorTests.cs` file created
- [ ] Test for Activity creation with correct name
- [ ] Test for zero overhead when OTEL not configured
- [ ] Test for error status on exception
- [ ] Uses ActivityListener for verification
- [ ] All tests pass
- [ ] Tests use FluentAssertions
- [ ] Tests in namespace: `Kommand.Tests.Unit`

---

### Task 3.6: Write Unit Tests for MetricsInterceptor
**Status**: [ ] Not Started

**Objective**: Test metrics recording behavior (Architecture Doc Section 12).

**Instructions**:
1. Create test file `Unit/MetricsInterceptorTests.cs` in `Kommand.Tests` project:
   ```csharp
   namespace Kommand.Tests.Unit;

   using System.Diagnostics.Metrics;
   using FluentAssertions;

   public class MetricsInterceptorTests
   {
       [Fact]
       public async Task HandleAsync_RecordsSuccessMetrics()
       {
           // Arrange
           var interceptor = new MetricsInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => Task.FromResult("result");

           // Act
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           result.Should().Be("result");
           // Note: Testing Meter behavior requires MeterListener setup
       }

       [Fact]
       public async Task HandleAsync_OnException_RecordsFailureMetrics()
       {
           // Arrange
           var interceptor = new MetricsInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test error");

           // Act & Assert
           await Assert.ThrowsAsync<InvalidOperationException>(
               async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

           // Metrics should be recorded with status="failure"
       }

       [Fact]
       public async Task HandleAsync_RecordsDuration()
       {
           // Arrange
           var interceptor = new MetricsInterceptor<TestCommand, string>();
           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = async () =>
           {
               await Task.Delay(10); // Simulate work
               return "result";
           };

           // Act
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           result.Should().Be("result");
           // Duration should be recorded (>= 10ms)
       }
   }

   // Test fixtures reused from ActivityInterceptorTests
   ```
2. Add MeterListener setup if needed for detailed metric verification
3. Reference .NET documentation for Meter testing patterns

**How it fits**: Ensures metrics collection works correctly.

**Builds upon**:
- Task 3.2 (MetricsInterceptor)
- Task 3.5 (OTEL testing patterns)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] `Unit/MetricsInterceptorTests.cs` file created
- [ ] Test for success metrics recording
- [ ] Test for failure metrics recording
- [ ] Test for duration measurement
- [ ] All tests pass
- [ ] Tests use FluentAssertions
- [ ] Tests in namespace: `Kommand.Tests.Unit`

---

## Phase 4: Validation System (7 tasks)

### Task 4.1: Implement Validation Abstractions - Part 1 (IValidator Interface)
**Status**: [ ] Not Started

**Objective**: Create the validation interface (Architecture Doc Section 6, lines 668-671).

**Instructions**:
1. In `src/Kommand/Validation/`, create `IValidator.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Validator for a request type.
   /// Validators are automatically discovered during assembly scanning.
   /// </summary>
   /// <typeparam name="T">The request type to validate</typeparam>
   public interface IValidator<in T>
   {
       /// <summary>
       /// Validates the request asynchronously.
       /// </summary>
       /// <param name="instance">The instance to validate</param>
       /// <param name="cancellationToken">Cancellation token</param>
       /// <returns>Validation result indicating success or failure with errors</returns>
       Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken);
   }
   ```
2. Add comprehensive XML documentation explaining:
   - When to implement this interface
   - That validators are auto-discovered
   - That validators can inject dependencies (repositories, services)
   - That validation is async (supports DB queries, API calls)
   - Example validator implementation

**How it fits**: Foundation for custom validation without external dependencies.

**Builds upon**: Task 1.2 (Validation folder structure)

**Future dependencies**:
- Task 4.2 (ValidationResult)
- Task 4.4 (ValidationInterceptor)
- Task 4.5 (auto-discovery in assembly scanning)

**Completion Checklist**:
- [ ] `IValidator<T>` interface created in `Validation/` folder
- [ ] Method named `ValidateAsync` (not just `Validate`)
- [ ] Takes instance and CancellationToken parameters
- [ ] Returns `Task<ValidationResult>`
- [ ] Contravariant `in` modifier on T
- [ ] Comprehensive XML documentation
- [ ] Example implementation in XML comments
- [ ] Project builds successfully

---

### Task 4.2: Implement Validation Abstractions - Part 2 (Result Types)
**Status**: [ ] Not Started

**Objective**: Create validation result types (Architecture Doc Section 6, lines 673-683).

**Instructions**:
1. In `src/Kommand/Validation/`, create `ValidationResult.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Result of a validation operation.
   /// </summary>
   public class ValidationResult
   {
       /// <summary>
       /// Indicates whether validation succeeded.
       /// </summary>
       public bool IsValid { get; init; }

       /// <summary>
       /// Collection of validation errors (empty if valid).
       /// </summary>
       public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

       /// <summary>
       /// Creates a successful validation result.
       /// </summary>
       public static ValidationResult Success() => new() { IsValid = true };

       /// <summary>
       /// Creates a failed validation result with errors.
       /// </summary>
       public static ValidationResult Failure(params ValidationError[] errors)
           => new() { IsValid = false, Errors = errors };
   }
   ```
2. Create `ValidationError.cs` in same directory:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Represents a single validation error.
   /// </summary>
   /// <param name="PropertyName">The name of the property that failed validation</param>
   /// <param name="ErrorMessage">The error message</param>
   public record ValidationError(string PropertyName, string ErrorMessage);
   ```
3. Add comprehensive XML documentation

**How it fits**: Provides result types for validation operations.

**Builds upon**: Task 4.1 (IValidator interface)

**Future dependencies**:
- Task 4.3 (ValidationException)
- Task 4.4 (ValidationInterceptor)

**Completion Checklist**:
- [ ] `ValidationResult` class created
- [ ] `IsValid` property with init accessor
- [ ] `Errors` property with init accessor and default empty array
- [ ] `Success()` factory method
- [ ] `Failure()` factory method accepting params array
- [ ] `ValidationError` record created
- [ ] Record has PropertyName and ErrorMessage parameters
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 4.3: Implement Validation Abstractions - Part 3 (ValidationException)
**Status**: [ ] Not Started

**Objective**: Create exception type for validation failures (Architecture Doc Section 6, lines 795-805).

**Instructions**:
1. In `src/Kommand/Validation/`, create `ValidationException.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Exception thrown when validation fails.
   /// Contains all validation errors from all validators.
   /// </summary>
   public class ValidationException : Exception
   {
       /// <summary>
       /// Gets the validation errors.
       /// </summary>
       public IReadOnlyList<ValidationError> Errors { get; }

       /// <summary>
       /// Initializes a new instance with validation errors.
       /// </summary>
       /// <param name="errors">The validation errors</param>
       public ValidationException(IReadOnlyList<ValidationError> errors)
           : base($"Validation failed with {errors.Count} error(s)")
       {
           Errors = errors ?? throw new ArgumentNullException(nameof(errors));
       }
   }
   ```
2. Add comprehensive XML documentation explaining:
   - When this exception is thrown (by ValidationInterceptor)
   - That it contains all errors from all validators
   - How to catch and handle it (e.g., in ASP.NET Core middleware)

**How it fits**: Allows validation failures to propagate with detailed error information.

**Builds upon**: Task 4.2 (ValidationError)

**Future dependencies**: Task 4.4 (ValidationInterceptor throws this)

**Completion Checklist**:
- [ ] `ValidationException` class created
- [ ] Inherits from `Exception`
- [ ] `Errors` property is readonly
- [ ] Constructor accepts IReadOnlyList<ValidationError>
- [ ] Constructor validates errors parameter (not null)
- [ ] Base message includes error count
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 4.4: Implement ValidationInterceptor
**Status**: [ ] Not Started

**Objective**: Create built-in interceptor that runs validators (Architecture Doc Section 6, lines 743-793).

**Instructions**:
1. In `src/Kommand/Validation/`, create `ValidationInterceptor.cs`:
   ```csharp
   namespace Kommand;

   using Microsoft.Extensions.Logging;

   /// <summary>
   /// Built-in interceptor that runs all validators for a request.
   /// Automatically discovers and executes validators registered in DI.
   /// </summary>
   public class ValidationInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       private readonly IEnumerable<IValidator<TRequest>> _validators;
       private readonly ILogger<ValidationInterceptor<TRequest, TResponse>> _logger;

       public ValidationInterceptor(
           IEnumerable<IValidator<TRequest>> validators,
           ILogger<ValidationInterceptor<TRequest, TResponse>> logger)
       {
           _validators = validators ?? throw new ArgumentNullException(nameof(validators));
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
       }

       public async Task<TResponse> HandleAsync(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken)
       {
           // Skip if no validators registered for this request type
           if (!_validators.Any())
               return await next();

           var errors = new List<ValidationError>();

           // Execute all validators
           foreach (var validator in _validators)
           {
               var result = await validator.ValidateAsync(request, cancellationToken);
               if (!result.IsValid)
               {
                   errors.AddRange(result.Errors);
                   _logger.LogWarning(
                       "Validation failed for {RequestType}: {ErrorCount} errors",
                       typeof(TRequest).Name,
                       result.Errors.Count);
               }
           }

           // If any errors, throw ValidationException (short-circuit)
           if (errors.Any())
           {
               _logger.LogError(
                   "Validation failed for {RequestType} with {ErrorCount} total errors",
                   typeof(TRequest).Name,
                   errors.Count);
               throw new ValidationException(errors);
           }

           // Validation passed, continue to handler
           return await next();
       }
   }
   ```
2. Add NuGet package reference to `Kommand.csproj`:
   ```xml
   <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
   ```
3. Add comprehensive XML documentation explaining:
   - How validators are discovered (via IEnumerable<IValidator<T>>)
   - That validation runs sequentially (not parallel)
   - That ALL validators run (collects all errors)
   - That it short-circuits (doesn't call next()) if validation fails

**How it fits**: Automatically runs validation before handler execution when enabled.

**Builds upon**:
- Task 4.1-4.3 (validation abstractions)
- Task 2.1 (IInterceptor interface)

**Future dependencies**:
- Task 4.5 (auto-discovery)
- Task 4.6 (WithValidation() registration)

**Completion Checklist**:
- [ ] ValidationInterceptor class created in `Validation/` folder
- [ ] Implements IInterceptor<TRequest, TResponse>
- [ ] Injects IEnumerable<IValidator<TRequest>>
- [ ] Injects ILogger
- [ ] Skips validation if no validators
- [ ] Executes all validators sequentially
- [ ] Collects all errors from all validators
- [ ] Logs warnings for each failed validator
- [ ] Logs error for total failure
- [ ] Throws ValidationException with all errors
- [ ] Short-circuits (doesn't call next()) on failure
- [ ] Microsoft.Extensions.Logging.Abstractions package added
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 4.5: Update Assembly Scanning for Validators (Complete Implementation)
**Status**: [ ] Not Started

**Objective**: Complete the validator auto-discovery that was placeholder in Task 1.13 (Architecture Doc Section 6, lines 831-834).

**Instructions**:
1. In `src/Kommand/Registration/KommandConfiguration.cs` (from Task 1.13), update the validator scanning logic:
   ```csharp
   // Scan for validators (now IValidator<T> exists!)
   var validators = assembly.GetTypes()
       .Where(t => t.IsClass && !t.IsAbstract)
       .Where(t => t.GetInterfaces().Any(i =>
           i.IsGenericType &&
           i.GetGenericTypeDefinition() == typeof(IValidator<>)))
       .ToList();

   foreach (var validatorType in validators)
   {
       var interfaces = validatorType.GetInterfaces()
           .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

       foreach (var @interface in interfaces)
       {
           // Validators always Scoped (can inject repositories)
           _validatorDescriptors.Add(new ServiceDescriptor(@interface, validatorType, ServiceLifetime.Scoped));
       }
   }
   ```
2. Replace the placeholder code that used `.Name == "IValidator`1"` with proper type checking
3. Verify that validators are registered with Scoped lifetime (not Transient or Singleton)

**How it fits**: Completes the auto-discovery feature for validators.

**Builds upon**:
- Task 1.13 (placeholder implementation)
- Task 4.1 (IValidator interface now exists)

**Future dependencies**: Task 4.6 (WithValidation() needs this)

**Completion Checklist**:
- [ ] Validator scanning code updated in RegisterHandlersFromAssembly()
- [ ] Uses proper type checking: `typeof(IValidator<>)`
- [ ] Removes placeholder string-based checking
- [ ] Registers validators as Scoped
- [ ] Supports multiple validators per request type
- [ ] Project builds successfully
- [ ] No compilation errors

---

### Task 4.6: Implement WithValidation() Fluent Helper (Complete Implementation)
**Status**: [ ] Not Started

**Objective**: Complete the WithValidation() method that was placeholder in Task 1.13 (Architecture Doc Section 6, lines 731-732).

**Instructions**:
1. In `src/Kommand/Registration/KommandConfiguration.cs`, update `WithValidation()` method:
   ```csharp
   /// <summary>
   /// Enables validation by adding ValidationInterceptor.
   /// Validators must be in assemblies registered via RegisterHandlersFromAssembly().
   /// </summary>
   public KommandConfiguration WithValidation()
   {
       AddInterceptor(typeof(ValidationInterceptor<,>));
       return this;
   }
   ```
2. Update XML documentation to explain:
   - That this adds ValidationInterceptor to the pipeline
   - That validators must be in scanned assemblies
   - Usage example in XML comments
3. This replaces the placeholder implementation from Task 1.13

**How it fits**: Provides simple opt-in for validation without manual interceptor registration.

**Builds upon**:
- Task 4.4 (ValidationInterceptor now exists)
- Task 1.13 (placeholder implementation)

**Future dependencies**: Task 4.7 (testing)

**Completion Checklist**:
- [ ] WithValidation() method updated
- [ ] Adds ValidationInterceptor<,> to interceptor types
- [ ] Returns this for chaining
- [ ] Comprehensive XML documentation
- [ ] Usage example in XML comments
- [ ] Project builds successfully

---

### Task 4.7: Write Tests for Validation System
**Status**: [ ] Not Started

**Objective**: Test validation execution and error handling (Architecture Doc Section 12).

**Instructions**:
1. Create test file `Unit/ValidationTests.cs` in `Kommand.Tests` project:
   ```csharp
   namespace Kommand.Tests.Unit;

   using Microsoft.Extensions.DependencyInjection;
   using Microsoft.Extensions.Logging.Abstractions;
   using FluentAssertions;

   public class ValidationTests
   {
       [Fact]
       public async Task ValidationInterceptor_WithNoValidators_CallsNext()
       {
           // Arrange
           var interceptor = new ValidationInterceptor<TestCommand, string>(
               Enumerable.Empty<IValidator<TestCommand>>(),
               NullLogger<ValidationInterceptor<TestCommand, string>>.Instance);

           var command = new TestCommand("test");
           var nextCalled = false;
           RequestHandlerDelegate<string> next = () =>
           {
               nextCalled = true;
               return Task.FromResult("result");
           };

           // Act
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           nextCalled.Should().BeTrue();
           result.Should().Be("result");
       }

       [Fact]
       public async Task ValidationInterceptor_WithValidRequest_CallsNext()
       {
           // Arrange
           var validator = new TestCommandValidator(isValid: true);
           var interceptor = new ValidationInterceptor<TestCommand, string>(
               new[] { validator },
               NullLogger<ValidationInterceptor<TestCommand, string>>.Instance);

           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => Task.FromResult("result");

           // Act
           var result = await interceptor.HandleAsync(command, next, CancellationToken.None);

           // Assert
           result.Should().Be("result");
       }

       [Fact]
       public async Task ValidationInterceptor_WithInvalidRequest_ThrowsValidationException()
       {
           // Arrange
           var validator = new TestCommandValidator(isValid: false);
           var interceptor = new ValidationInterceptor<TestCommand, string>(
               new[] { validator },
               NullLogger<ValidationInterceptor<TestCommand, string>>.Instance);

           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => Task.FromResult("result");

           // Act & Assert
           var exception = await Assert.ThrowsAsync<ValidationException>(
               async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

           exception.Errors.Should().HaveCount(1);
           exception.Errors[0].PropertyName.Should().Be("Value");
           exception.Errors[0].ErrorMessage.Should().Be("Test validation error");
       }

       [Fact]
       public async Task ValidationInterceptor_WithMultipleValidators_CollectsAllErrors()
       {
           // Arrange
           var validator1 = new TestCommandValidator(isValid: false, errorMessage: "Error 1");
           var validator2 = new TestCommandValidator(isValid: false, errorMessage: "Error 2");
           var interceptor = new ValidationInterceptor<TestCommand, string>(
               new[] { validator1, validator2 },
               NullLogger<ValidationInterceptor<TestCommand, string>>.Instance);

           var command = new TestCommand("test");
           RequestHandlerDelegate<string> next = () => Task.FromResult("result");

           // Act & Assert
           var exception = await Assert.ThrowsAsync<ValidationException>(
               async () => await interceptor.HandleAsync(command, next, CancellationToken.None));

           exception.Errors.Should().HaveCount(2);
       }

       [Fact]
       public async Task EndToEnd_WithValidation_ValidatesBeforeHandler()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddLogging();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
               config.WithValidation();
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();

           var invalidCommand = new TestCommand(""); // Empty value should fail validation

           // Act & Assert
           await Assert.ThrowsAsync<ValidationException>(
               async () => await mediator.SendAsync(invalidCommand, CancellationToken.None));
       }
   }

   // Test fixtures
   public record TestCommand(string Value) : ICommand<string>;

   public class TestCommandHandler : ICommandHandler<TestCommand, string>
   {
       public Task<string> HandleAsync(TestCommand command, CancellationToken cancellationToken)
       {
           return Task.FromResult(command.Value + "-handled");
       }
   }

   public class TestCommandValidator : IValidator<TestCommand>
   {
       private readonly bool _isValid;
       private readonly string _errorMessage;

       public TestCommandValidator(bool isValid, string errorMessage = "Test validation error")
       {
           _isValid = isValid;
           _errorMessage = errorMessage;
       }

       public Task<ValidationResult> ValidateAsync(TestCommand instance, CancellationToken cancellationToken)
       {
           if (_isValid)
               return Task.FromResult(ValidationResult.Success());

           var error = new ValidationError("Value", _errorMessage);
           return Task.FromResult(ValidationResult.Failure(error));
       }
   }

   // Real validator for end-to-end test
   public class RealTestCommandValidator : IValidator<TestCommand>
   {
       public Task<ValidationResult> ValidateAsync(TestCommand instance, CancellationToken cancellationToken)
       {
           if (string.IsNullOrWhiteSpace(instance.Value))
           {
               var error = new ValidationError(nameof(instance.Value), "Value is required");
               return Task.FromResult(ValidationResult.Failure(error));
           }

           return Task.FromResult(ValidationResult.Success());
       }
   }
   ```
2. Run tests: `dotnet test`

**How it fits**: Ensures validation system works correctly end-to-end.

**Builds upon**:
- Task 4.4 (ValidationInterceptor)
- Task 4.6 (WithValidation)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] `Unit/ValidationTests.cs` file created
- [ ] Test for no validators (passes through)
- [ ] Test for valid request (passes through)
- [ ] Test for invalid request (throws ValidationException)
- [ ] Test for multiple validators (collects all errors)
- [ ] End-to-end test with real DI container
- [ ] Test fixtures created
- [ ] All tests pass
- [ ] Tests use FluentAssertions
- [ ] Tests in namespace: `Kommand.Tests.Unit`

---

## Phase 5: Polish & Optimization (8 tasks)

### Task 5.1: Create Exceptions Directory and Custom Exceptions
**Status**: [ ] Not Started

**Objective**: Create custom exception types for better error handling (Architecture Doc Section 5, lines 591-594).

**Instructions**:
1. In `src/Kommand/Exceptions/`, create `KommandException.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Base exception for all Kommand-specific exceptions.
   /// </summary>
   public abstract class KommandException : Exception
   {
       protected KommandException(string message) : base(message) { }
       protected KommandException(string message, Exception innerException) : base(message, innerException) { }
   }
   ```
2. Create `HandlerNotFoundException.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Exception thrown when no handler is registered for a request.
   /// </summary>
   public class HandlerNotFoundException : KommandException
   {
       public Type RequestType { get; }

       public HandlerNotFoundException(Type requestType)
           : base($"No handler registered for request type: {requestType.Name}")
       {
           RequestType = requestType;
       }
   }
   ```
3. Update Mediator.cs to throw HandlerNotFoundException instead of InvalidOperationException
4. Add comprehensive XML documentation

**How it fits**: Provides strongly-typed exceptions for better error handling and debugging.

**Builds upon**: Task 1.9-1.11 (Mediator implementation)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] `KommandException` base class created
- [ ] `HandlerNotFoundException` class created
- [ ] Mediator updated to throw HandlerNotFoundException
- [ ] All three dispatch methods (SendAsync, QueryAsync, PublishAsync) updated
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully
- [ ] Tests still pass

---

### Task 5.2: Add XML Documentation to All Public APIs
**Status**: [ ] Not Started

**Objective**: Ensure all public types have comprehensive XML documentation (Architecture Doc Section 13).

**Instructions**:
1. Review all public interfaces, classes, and methods
2. Ensure each has:
   - `<summary>` explaining what it does
   - `<remarks>` with usage notes (if needed)
   - `<example>` with code sample (where helpful)
   - `<param>` for all parameters
   - `<returns>` for return values
   - `<exception>` for thrown exceptions
3. Pay special attention to:
   - All abstractions (ICommand, IQuery, IMediator, etc.)
   - All interceptor interfaces
   - ValidationInterceptor and validation types
   - KommandConfiguration
   - ServiceCollectionExtensions
4. Build project and check for missing XML doc warnings: `dotnet build`
5. Fix all warnings (TreatWarningsAsErrors=true should catch these)

**How it fits**: Provides IntelliSense documentation for users of the library.

**Builds upon**: All previous tasks

**Future dependencies**: Task 6.2 (documentation generation)

**Completion Checklist**:
- [ ] All public types reviewed
- [ ] All public types have <summary>
- [ ] All public methods have <summary>
- [ ] All parameters have <param>
- [ ] All return values have <returns>
- [ ] Examples provided where helpful
- [ ] Build produces zero XML documentation warnings
- [ ] XML doc file generated in output

---

### Task 5.3: Create Complete Sample Project
**Status**: [ ] Not Started

**Objective**: Create comprehensive sample showing all features (Architecture Doc Section 14, Phase 6, lines 1446-1449).

**Instructions**:
1. Update `samples/Kommand.Sample/` project (created in Task 2.4)
2. Add `Program.cs` with full DI setup:
   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   // Add Kommand with all features
   builder.Services.AddKommand(config =>
   {
       config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
       config.WithValidation();
       config.AddInterceptor<LoggingInterceptor>();
   });

   // Add OTEL (optional - shows auto-integration)
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
       .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

   var app = builder.Build();
   app.MapControllers();
   app.Run();
   ```
3. Create example commands, queries, handlers:
   - `Commands/CreateUserCommand.cs`
   - `Queries/GetUserQuery.cs`
   - `Handlers/CreateUserCommandHandler.cs`
   - `Handlers/GetUserQueryHandler.cs`
   - `Validators/CreateUserCommandValidator.cs`
   - `Notifications/UserCreatedNotification.cs`
   - `Handlers/UserCreatedNotificationHandler.cs`
4. Create example controller:
   - `Controllers/UsersController.cs` showing IMediator usage
5. Add comprehensive comments explaining each part
6. Add README.md in samples directory explaining what the sample demonstrates

**How it fits**: Provides working reference implementation for users.

**Builds upon**: All previous implementation tasks

**Future dependencies**: Task 6.2 (documentation references this)

**Completion Checklist**:
- [ ] Program.cs with complete DI setup
- [ ] Example command and handler
- [ ] Example query and handler
- [ ] Example validator
- [ ] Example notification and handler
- [ ] Example controller using IMediator
- [ ] LoggingInterceptor from Task 2.4 used
- [ ] README.md explaining the sample
- [ ] Comprehensive comments throughout
- [ ] Sample builds and runs successfully

---

### Task 5.4: Create BenchmarkDotNet Project
**Status**: [ ] Not Started

**Objective**: Set up performance benchmarking (Architecture Doc Section 14, Phase 6, lines 1441-1444).

**Instructions**:
1. Create benchmark project:
   ```bash
   cd tests
   dotnet new console -n Kommand.Benchmarks
   cd ..
   dotnet sln add tests/Kommand.Benchmarks
   cd tests/Kommand.Benchmarks
   dotnet add reference ../../src/Kommand/Kommand.csproj
   dotnet add package BenchmarkDotNet
   ```
2. Create `KommandBenchmarks.cs`:
   ```csharp
   using BenchmarkDotNet.Attributes;
   using BenchmarkDotNet.Running;
   using Microsoft.Extensions.DependencyInjection;

   namespace Kommand.Benchmarks;

   [MemoryDiagnoser]
   [SimpleJob(RuntimeMoniker.Net80)]
   public class KommandBenchmarks
   {
       private IMediator _mediator = null!;
       private IServiceProvider _provider = null!;
       private TestCommand _command = null!;

       [GlobalSetup]
       public void Setup()
       {
           var services = new ServiceCollection();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
           });
           _provider = services.BuildServiceProvider();
           _mediator = _provider.GetRequiredService<IMediator>();
           _command = new TestCommand("test");
       }

       [Benchmark(Baseline = true)]
       public async Task<string> DirectHandlerCall()
       {
           var handler = new TestCommandHandler();
           return await handler.HandleAsync(_command, CancellationToken.None);
       }

       [Benchmark]
       public async Task<string> KommandWithoutInterceptors()
       {
           return await _mediator.SendAsync(_command, CancellationToken.None);
       }

       [Benchmark]
       public async Task<string> KommandWith3Interceptors()
       {
           // Setup mediator with 3 interceptors
           // Benchmark overhead of interceptor pipeline
           return await _mediator.SendAsync(_command, CancellationToken.None);
       }
   }

   public record TestCommand(string Value) : ICommand<string>;

   public class TestCommandHandler : ICommandHandler<TestCommand, string>
   {
       public Task<string> HandleAsync(TestCommand command, CancellationToken ct)
       {
           return Task.FromResult(command.Value + "-handled");
       }
   }

   class Program
   {
       static void Main(string[] args)
       {
           BenchmarkRunner.Run<KommandBenchmarks>();
       }
   }
   ```
3. Add instructions in comments on how to run benchmarks
4. Document performance targets from Architecture Doc Section 11, lines 1166-1173

**How it fits**: Validates performance targets are met.

**Builds upon**: All Phase 1-4 implementation

**Future dependencies**: Task 6.4 (performance results in docs)

**Completion Checklist**:
- [ ] Kommand.Benchmarks project created
- [ ] BenchmarkDotNet package added
- [ ] KommandBenchmarks class with 3 benchmarks
- [ ] Direct call baseline benchmark
- [ ] Kommand without interceptors benchmark
- [ ] Kommand with interceptors benchmark
- [ ] Memory diagnostics enabled
- [ ] Benchmarks run successfully
- [ ] Results documented

---

### Task 5.5: Run Full Test Suite and Measure Coverage
**Status**: [ ] Not Started

**Objective**: Verify all tests pass and coverage meets requirements (Architecture Doc Section 12).

**Instructions**:
1. Install coverage tool if not already installed:
   ```bash
   dotnet tool install --global coverlet.console
   ```
2. Run tests with coverage:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover
   ```
3. Verify coverage thresholds:
   - Overall coverage > 80%
   - Core components (Mediator, interceptor pipeline) > 90%
   - Critical paths (handler resolution, validation) > 90%
4. Generate HTML coverage report:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   reportgenerator -reports:tests/Kommand.Tests/coverage.opencover.xml -targetdir:coverage-report
   ```
5. Review uncovered lines
6. Add tests if needed to reach thresholds
7. Document final coverage numbers in coverage-report/

**How it fits**: Ensures production-ready quality with comprehensive testing.

**Builds upon**: All test tasks from Phases 1-4

**Future dependencies**: Task 6.4 (coverage badge in README)

**Completion Checklist**:
- [ ] Coverage tool installed
- [ ] All tests run successfully
- [ ] Overall coverage > 80%
- [ ] Core components coverage > 90%
- [ ] HTML coverage report generated
- [ ] Coverage gaps reviewed
- [ ] Additional tests added if needed
- [ ] Final coverage numbers documented

---

### Task 5.6: Optimize Assembly Scanning
**Status**: [ ] Not Started

**Objective**: Optimize handler/validator discovery performance (Architecture Doc Section 14, Phase 6, lines 1437-1440).

**Instructions**:
1. In `KommandConfiguration.RegisterHandlersFromAssembly()`, add caching:
   ```csharp
   // Cache to avoid repeated reflection
   private static readonly ConcurrentDictionary<Assembly, List<ServiceDescriptor>> AssemblyScanCache = new();

   public KommandConfiguration RegisterHandlersFromAssembly(
       Assembly assembly,
       ServiceLifetime? lifetime = null)
   {
       if (assembly == null) throw new ArgumentNullException(nameof(assembly));

       // Check cache first
       if (AssemblyScanCache.TryGetValue(assembly, out var cachedDescriptors))
       {
           _handlerDescriptors.AddRange(cachedDescriptors);
           return this;
       }

       // ... existing scanning logic ...

       // Cache results
       AssemblyScanCache[assembly] = _handlerDescriptors.ToList();

       return this;
   }
   ```
2. Add benchmarks to verify optimization improves startup time
3. Document optimization in XML comments

**How it fits**: Improves application startup time when scanning large assemblies.

**Builds upon**: Task 1.13 (assembly scanning implementation)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] Caching implemented for assembly scanning
- [ ] Uses ConcurrentDictionary for thread safety
- [ ] Benchmark shows improvement
- [ ] XML documentation updated
- [ ] Project builds successfully
- [ ] Tests still pass

---

### Task 5.7: Add Opt-In for Interceptor Order
**Status**: [ ] Not Started

**Objective**: Allow users to control interceptor execution order explicitly.

**Instructions**:
1. In `KommandConfiguration.cs`, update `AddInterceptor()` to track order:
   ```csharp
   /// <summary>
   /// Adds an interceptor with specific order priority.
   /// Lower priority values execute first (outer interceptors).
   /// </summary>
   public KommandConfiguration AddInterceptor<TInterceptor>(int priority = 0)
       where TInterceptor : class
   {
       _interceptorTypes.Add((typeof(TInterceptor), priority));
       return this;
   }
   ```
2. Update ServiceCollectionExtensions to sort interceptors by priority
3. Add XML documentation explaining execution order
4. Add tests verifying order control

**How it fits**: Gives users fine-grained control over interceptor pipeline.

**Builds upon**: Task 2.2 (interceptor pipeline)

**Future dependencies**: None.

**Completion Checklist**:
- [ ] AddInterceptor accepts priority parameter
- [ ] Interceptors sorted by priority before registration
- [ ] XML documentation explains priority system
- [ ] Tests verify order control
- [ ] Project builds successfully

---

### Task 5.8: Create CHANGELOG.md
**Status**: [ ] Not Started

**Objective**: Document version history and changes.

**Instructions**:
1. Create `CHANGELOG.md` at repository root
2. Follow Keep a Changelog format
3. Document v1.0.0 initial release:
   ```markdown
   # Changelog

   All notable changes to Kommand will be documented in this file.

   The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
   and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

   ## [1.0.0] - 2025-MM-DD

   ### Added
   - Initial release
   - CQRS support with ICommand and IQuery
   - Interceptor system for cross-cutting concerns
   - Automatic OpenTelemetry integration (zero-config)
   - Built-in validation system with auto-discovery
   - Pub/sub notifications
   - Comprehensive testing (>80% coverage)
   - Full XML documentation

   ### Features
   - Zero external dependencies (except DI abstractions and OTEL primitives)
   - MIT License
   - Targets .NET 8 LTS (forward compatible with .NET 9, 10+)
   - Auto-discovery of handlers and validators
   ```
4. Add sections for Future, Unreleased, etc.

**How it fits**: Provides version history for users and maintainers.

**Builds upon**: All implementation tasks

**Future dependencies**: Task 6.5 (release process)

**Completion Checklist**:
- [ ] CHANGELOG.md created at root
- [ ] Follows Keep a Changelog format
- [ ] v1.0.0 documented
- [ ] All major features listed
- [ ] Date format consistent
- [ ] File committed to repository

---

## Phase 6: Documentation & Release (8 tasks)

### Task 6.1: Create README.md
**Status**: [ ] Not Started

**Objective**: Create comprehensive README for the repository (Architecture Doc Section 13).

**Instructions**:
1. Create `README.md` at repository root
2. Include sections:
   ```markdown
   # Kommand

   A lightweight, production-ready CQRS mediator for .NET 8+ with built-in OpenTelemetry support.

   ![.NET](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4)
   ![Forward Compatible](https://img.shields.io/badge/compatible-.NET%208%2C%209%2C%2010%2B-success)

   ## Features

   - ✅ **CQRS**: Explicit command and query separation
   - ✅ **Zero Dependencies**: Only DI abstractions (no commercial libraries)
   - ✅ **Auto-Discovery**: Handlers and validators automatically registered
   - ✅ **Interceptors**: Cross-cutting concerns (validation, logging, etc.)
   - ✅ **OpenTelemetry**: Zero-config distributed tracing and metrics
   - ✅ **Pub/Sub**: Domain events with multiple handlers
   - ✅ **MIT License**: Fully open source

   ## Quick Start

   ### Installation

   ```bash
   dotnet add package Kommand
   ```

   ### Basic Usage

   [Code examples showing command, handler, registration, usage]

   ## Documentation

   - [Getting Started](docs/getting-started.md)
   - [Architecture](MEDIATOR_ARCHITECTURE_PLAN.md)
   - [Migration from MediatR](docs/migration-from-mediatr.md)
   - [API Reference](docs/api-reference.md)

   ## Why Kommand?

   [Explanation of licensing concerns, zero dependencies, etc.]

   ## Contributing

   [Link to CONTRIBUTING.md]

   ## License

   MIT License - see [LICENSE](LICENSE) for details
   ```
3. Add badges: build status, NuGet version, license, coverage
4. Add concise code examples
5. Keep it focused on getting started quickly

**How it fits**: First impression for users discovering the library.

**Builds upon**: All implementation and Task 5.3 (sample project)

**Future dependencies**: Task 6.5 (badges will be updated with real data)

**Completion Checklist**:
- [ ] README.md created at root
- [ ] All sections included
- [ ] Features list complete
- [ ] Quick start example works
- [ ] Badge placeholders added
- [ ] Links to other docs
- [ ] Proofread for clarity
- [ ] Markdown renders correctly

---

### Task 6.2: Create Getting Started Guide
**Status**: [ ] Not Started

**Objective**: Create detailed getting started documentation.

**Instructions**:
1. Create `docs/getting-started.md`
2. Include step-by-step guide:
   - Installation
   - Creating first command
   - Creating handler
   - Registering with DI
   - Using IMediator
   - Adding validation
   - Adding custom interceptor
   - Configuring OTEL (optional)
3. Include complete code examples that can be copied
4. Add troubleshooting section
5. Reference sample project from Task 5.3

**How it fits**: Detailed onboarding documentation.

**Builds upon**: All implementation

**Future dependencies**: None.

**Completion Checklist**:
- [ ] getting-started.md created
- [ ] Step-by-step guide complete
- [ ] All code examples tested
- [ ] Troubleshooting section
- [ ] Links to sample project
- [ ] Proofread for clarity

---

### Task 6.3: Create Migration from MediatR Guide
**Status**: [ ] Not Started

**Objective**: Help users migrate from MediatR to Kommand (Architecture Doc Section 17).

**Instructions**:
1. Create `docs/migration-from-mediatr.md`
2. Include:
   - Side-by-side comparison table
   - Terminology mapping (Pipeline Behaviors → Interceptors)
   - Breaking changes (Scoped vs Transient default)
   - Step-by-step migration guide
   - Code examples (before/after)
3. Cover scenarios:
   - Command/handler migration
   - Validation migration (from FluentValidation)
   - Logging behavior migration
   - Notification migration
4. Add common migration pitfalls
5. Reference Architecture Doc Section 17

**How it fits**: Helps users migrate from MediatR.

**Builds upon**: Understanding of both libraries

**Future dependencies**: None.

**Completion Checklist**:
- [ ] migration-from-mediatr.md created
- [ ] Comparison table complete
- [ ] Terminology mapping clear
- [ ] Migration steps detailed
- [ ] All scenarios covered
- [ ] Pitfalls documented
- [ ] Proofread for clarity

---

### Task 6.4: Configure NuGet Package
**Status**: [ ] Not Started

**Objective**: Prepare project for NuGet publishing (Architecture Doc Section 14, Phase 7, lines 1461-1466).

**Instructions**:
1. Update `Kommand.csproj` with package metadata:
   ```xml
   <PropertyGroup>
     <PackageId>Kommand</PackageId>
     <Version>1.0.0</Version>
     <Authors>Your Name or Atherio</Authors>
     <Company>Your Company or Atherio</Company>
     <Description>A lightweight, production-ready CQRS mediator for .NET 8+ with built-in OpenTelemetry support. Targets .NET 8 LTS (forward compatible with .NET 9, 10+). Zero dependencies (except DI abstractions). MIT License.</Description>
     <PackageLicenseExpression>MIT</PackageLicenseExpression>
     <PackageProjectUrl>https://github.com/your-org/kommand</PackageProjectUrl>
     <RepositoryUrl>https://github.com/your-org/kommand.git</RepositoryUrl>
     <RepositoryType>git</RepositoryType>
     <PackageTags>cqrs;mediator;dotnet;opentelemetry;clean-architecture;ddd;validation</PackageTags>
     <PackageReadmeFile>README.md</PackageReadmeFile>
     <PackageIcon>icon.png</PackageIcon>
     <PackageReleaseNotes>See CHANGELOG.md for full release notes</PackageReleaseNotes>
     <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
     <PublishRepositoryUrl>true</PublishRepositoryUrl>
     <EmbedUntrackedSources>true</EmbedUntrackedSources>
     <IncludeSymbols>true</IncludeSymbols>
     <SymbolPackageFormat>snupkg</SymbolPackageFormat>
   </PropertyGroup>

   <ItemGroup>
     <None Include="..\..\README.md" Pack="true" PackagePath="\" />
     <None Include="..\..\icon.png" Pack="true" PackagePath="\" />
   </ItemGroup>
   ```
2. Create package icon (128x128 PNG) at repository root
3. Test package creation: `dotnet pack`
4. Inspect .nupkg contents
5. Verify all files included correctly

**How it fits**: Prepares library for NuGet distribution.

**Builds upon**: Task 6.1 (README), Task 5.8 (CHANGELOG)

**Future dependencies**: Task 6.5 (publishing)

**Completion Checklist**:
- [ ] All package metadata added to csproj
- [ ] Package icon created (128x128)
- [ ] README.md included in package
- [ ] dotnet pack succeeds
- [ ] .nupkg file inspected
- [ ] All files correct
- [ ] Symbols package (.snupkg) generated
- [ ] Metadata accurate

---

### Task 6.5: Create GitHub Actions CI/CD Pipeline
**Status**: [ ] Not Started

**Objective**: Automate build, test, and release (Architecture Doc Section 14, Phase 7, lines 1467-1471).

**Instructions**:
1. Create `.github/workflows/ci.yml`:
   ```yaml
   name: CI

   on:
     push:
       branches: [ main ]
     pull_request:
       branches: [ main ]

   jobs:
     build:
       runs-on: ubuntu-latest

       steps:
       - uses: actions/checkout@v4

       - name: Setup .NET
         uses: actions/setup-dotnet@v4
         with:
           dotnet-version: '9.0.x'

       - name: Restore dependencies
         run: dotnet restore

       - name: Build
         run: dotnet build --no-restore --configuration Release

       - name: Test
         run: dotnet test --no-build --configuration Release --verbosity normal /p:CollectCoverage=true /p:CoverageReportsFormat=opencover

       - name: Upload coverage
         uses: codecov/codecov-action@v3
         with:
           files: tests/Kommand.Tests/coverage.opencover.xml
   ```
2. Create `.github/workflows/release.yml`:
   ```yaml
   name: Release

   on:
     push:
       tags:
         - 'v*'

   jobs:
     release:
       runs-on: ubuntu-latest

       steps:
       - uses: actions/checkout@v4

       - name: Setup .NET
         uses: actions/setup-dotnet@v4
         with:
           dotnet-version: '9.0.x'

       - name: Restore dependencies
         run: dotnet restore

       - name: Build
         run: dotnet build --configuration Release

       - name: Test
         run: dotnet test --configuration Release

       - name: Pack
         run: dotnet pack --configuration Release --output ./artifacts

       - name: Push to NuGet
         run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json

       - name: Create GitHub Release
         uses: softprops/action-gh-release@v1
         with:
           files: ./artifacts/*.nupkg
           generate_release_notes: true
   ```
3. Add NuGet API key as GitHub secret
4. Test workflows

**How it fits**: Automates quality checks and publishing.

**Builds upon**: All previous tasks

**Future dependencies**: Task 6.7 (first release)

**Completion Checklist**:
- [ ] ci.yml workflow created
- [ ] release.yml workflow created
- [ ] Workflows test on push
- [ ] Coverage upload configured
- [ ] NuGet API key added as secret
- [ ] Workflows pass successfully
- [ ] Ready for first release

---

### Task 6.6: Create Contributing Guidelines
**Status**: [ ] Not Started

**Objective**: Document how to contribute to the project (Architecture Doc Section 14, Phase 7, lines 1472-1477).

**Instructions**:
1. Create `CONTRIBUTING.md` at repository root
2. Include sections:
   - Code of Conduct
   - How to report bugs
   - How to suggest features
   - Development setup
   - Pull request process
   - Coding standards
   - Testing requirements (80% coverage)
   - Documentation requirements
3. Reference Architecture Doc for standards
4. Add issue and PR templates:
   - `.github/ISSUE_TEMPLATE/bug_report.md`
   - `.github/ISSUE_TEMPLATE/feature_request.md`
   - `.github/PULL_REQUEST_TEMPLATE.md`

**How it fits**: Encourages community contributions.

**Builds upon**: All implementation standards

**Future dependencies**: None.

**Completion Checklist**:
- [ ] CONTRIBUTING.md created
- [ ] All sections complete
- [ ] Issue templates created
- [ ] PR template created
- [ ] Code of Conduct referenced
- [ ] Standards clearly documented
- [ ] Proofread for clarity

---

### Task 6.7: Prepare v1.0.0 Release
**Status**: [ ] Not Started

**Objective**: Execute first production release (Architecture Doc Section 14, Phase 7, lines 1478-1481).

**Instructions**:
1. Verify all previous tasks complete
2. Run full test suite: `dotnet test`
3. Verify coverage > 80%
4. Run benchmarks and document results
5. Update CHANGELOG.md with release date
6. Create git tag:
   ```bash
   git tag -a v1.0.0 -m "Release v1.0.0"
   git push origin v1.0.0
   ```
7. Verify GitHub Actions release workflow triggers
8. Verify package published to NuGet.org
9. Create GitHub release with release notes
10. Verify package is discoverable on NuGet

**How it fits**: Makes library publicly available.

**Builds upon**: All previous tasks

**Future dependencies**: Task 6.8 (announcement)

**Completion Checklist**:
- [ ] All tests pass
- [ ] Coverage verified > 80%
- [ ] Benchmarks documented
- [ ] CHANGELOG.md updated
- [ ] Git tag created
- [ ] GitHub Actions succeeded
- [ ] Package on NuGet.org
- [ ] GitHub release created
- [ ] Package searchable on NuGet
- [ ] All documentation links work

---

### Task 6.8: Announce Release
**Status**: [ ] Not Started

**Objective**: Share the release with the community (Architecture Doc Section 14, Phase 7, lines 1482-1486).

**Instructions**:
1. Write blog post announcing Kommand (optional)
2. Post to Reddit:
   - r/dotnet
   - r/csharp
3. Post to HackerNews (optional)
4. Share on social media:
   - Twitter/X
   - LinkedIn
5. Include in post:
   - What Kommand is
   - Why it exists (licensing concerns)
   - Key features
   - Link to GitHub
   - Link to NuGet
   - Getting started example
6. Be prepared to respond to questions/feedback

**How it fits**: Drives adoption and awareness.

**Builds upon**: Task 6.7 (release complete)

**Future dependencies**: None - project complete! 🎉

**Completion Checklist**:
- [ ] Blog post written (optional)
- [ ] Posted to r/dotnet
- [ ] Posted to r/csharp
- [ ] Posted to HackerNews (optional)
- [ ] Shared on Twitter/LinkedIn
- [ ] Initial feedback monitored
- [ ] Questions answered

---

## Summary

**Total Tasks in Part 2**: 29 tasks
**Estimated Total Time**: ~29 hours (1 hour per task average)
**Completion Rate**: 0% (0/29 complete)

### Phase Breakdown (Part 2)
- **Phase 3: OpenTelemetry Integration** - 6 tasks (~6 hours)
- **Phase 4: Validation System** - 7 tasks (~7 hours)
- **Phase 5: Polish & Optimization** - 8 tasks (~8 hours)
- **Phase 6: Documentation & Release** - 8 tasks (~8 hours)

### Combined Total (Part 1 + Part 2)
- **Total Tasks**: 52 tasks (23 detailed in Part 1, 29 in Part 2)
- **Total Estimated Time**: ~52 hours
- **Coverage**: All phases from Architecture Document

### Notes
1. **Continuation**: This document continues from `KOMMAND_TASK_LIST.md` (Phase 1-2 foundation)
2. **Accuracy**: Every task has been verified against `MEDIATOR_ARCHITECTURE_PLAN.md` for 100% accuracy
3. **Structure**: Maintains identical structure to Part 1 for consistency
4. **Test Project**: Uses single `Kommand.Tests` project established in Part 1
5. **Dependencies**: All task dependencies reference both Part 1 and Part 2 tasks

### When Complete
Upon completing all 52 tasks, you will have:
- ✅ Production-ready CQRS library
- ✅ Zero external dependencies (except DI abstractions)
- ✅ >80% test coverage
- ✅ Complete documentation
- ✅ Published to NuGet
- ✅ Open source (MIT License)
- ✅ Ready for community contributions

**Next Step**: Start with Task 3.1 (ActivityInterceptor) to continue implementation.