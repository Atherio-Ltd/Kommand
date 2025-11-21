# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Kommand** is a production-ready CQRS/Mediator library for .NET 8+. This is a **standalone open-source project** that will be published to NuGet under the MIT license.

**Current Status**: Phase 1 (Core Foundation) complete! All abstractions, mediator implementation, DI registration, and integration tests working. Ready for Phase 2 (Interceptor System).

## Git Workflow

**IMPORTANT**: Do NOT create git commits automatically. Only commit when explicitly asked by the user. The user will prompt when it's time to commit changes.

## Target Framework Strategy

**Single Target: `net8.0`**

Why this works:
- ✅ .NET has strong **forward compatibility** - libraries targeting net8.0 automatically work on .NET 8, 9, 10, 11+
- ✅ The compiler enforces net8.0 APIs only (can't accidentally use newer features)
- ✅ .NET 8 is the current **LTS** (Long Term Support, supported until November 2026)
- ✅ Simpler build, smaller package, easier maintenance than multi-targeting
- ✅ Build with any SDK >= 8.0 (e.g., .NET 8, 9, or 10 SDK)

**What users need:**
- Projects on .NET 8, 9, or 10+ can use this library
- No special configuration required

## Key Architecture Decisions

### Core Design Principles (from MEDIATOR_ARCHITECTURE_PLAN.md)

1. **Zero External Dependencies** - Only `Microsoft.Extensions.DependencyInjection.Abstractions` and `System.Diagnostics.DiagnosticSource`
2. **CQRS Semantics** - Explicit `ICommand` vs `IQuery` separation (not just generic `IRequest`)
3. **Scoped by Default** - Handlers registered as Scoped (NOT Transient like MediatR)
4. **Auto-Discovery** - Handlers and validators automatically discovered during assembly scanning
5. **Built-in OTEL** - OpenTelemetry integration with zero configuration (always safe, ~10-50ns overhead when not configured)
6. **Single Package** - Everything ships in one NuGet package: `Kommand`

### Why This Exists

Created as an MIT-licensed alternative to MediatR that will remain free and open-source. 

## Implementation Roadmap

**Total: 52 tasks across 6 phases (~52 hours)**

Detailed task lists are in:
- `KOMMAND_TASK_LIST.md` - Phase 1 (Core Foundation) & Phase 2 (Interceptors)
- `KOMMAND_TASK_LIST_PART2.md` - Phases 3-6 (OTEL, Validation, Polish, Release)

### Phase Status

- [x] **Phase 1: Core Foundation** (15 tasks) - ✅ COMPLETED
  - Create project structure, implement `ICommand`/`IQuery`/`IMediator`, basic handler resolution
- [ ] **Phase 2: Interceptor System** (8 tasks)
  - Implement interceptor pipeline for cross-cutting concerns
- [ ] **Phase 3: OpenTelemetry Integration** (6 tasks)
  - Built-in `ActivityInterceptor` and `MetricsInterceptor` with auto-registration
- [ ] **Phase 4: Validation System** (7 tasks)
  - Custom `IValidator<T>` interface with auto-discovery and `ValidationInterceptor`
- [ ] **Phase 5: Polish & Optimization** (8 tasks)
  - Exceptions, documentation, samples, benchmarks, assembly scanning optimization
- [ ] **Phase 6: Documentation & Release** (8 tasks)
  - README, guides, CI/CD, NuGet publishing

**Next Task**: Task 2.1 (Implement Interceptor Abstractions)

## Repository Structure (Target)

```
kommand/                              # Root
├── src/
│   └── Kommand/                      # Main library (single package)
│       ├── Unit.cs                   # Core Unit type (Kommand namespace)
│       ├── Abstractions/             # ICommand, IQuery, IMediator, etc. (Kommand.Abstractions namespace)
│       ├── Interceptors/             # IInterceptor, ActivityInterceptor, MetricsInterceptor (Kommand.Interceptors namespace)
│       ├── Validation/               # IValidator, ValidationResult, ValidationInterceptor (Kommand.Validation namespace)
│       ├── Implementation/           # Mediator class (internal) (Kommand.Implementation namespace)
│       ├── Registration/             # ServiceCollectionExtensions, KommandConfiguration (Kommand.Registration namespace)
│       └── Exceptions/               # Custom exception types (Kommand.Exceptions namespace)
├── tests/
│   └── Kommand.Tests/                # Single test project (Unit/ and Integration/ folders)
├── samples/
│   └── Kommand.Sample/               # Working example with all features
├── docs/                             # User documentation
├── MEDIATOR_ARCHITECTURE_PLAN.md     # Complete architecture (READ THIS FIRST)
├── KOMMAND_TASK_LIST.md              # Tasks 1-23
├── KOMMAND_TASK_LIST_PART2.md        # Tasks 24-52
└── README.md                         # Public-facing documentation

Target Framework: net8.0 (build with any SDK >= 8, works on .NET 8, 9, 10+)
Package: Kommand (single package, <50KB)
```

## Common Development Commands

### Build
```bash
dotnet build                          # Build entire solution
dotnet build -c Release               # Release build
```

### Test
```bash
dotnet test                           # Run all tests
dotnet test /p:CollectCoverage=true   # With coverage
```

### Package
```bash
dotnet pack -c Release                # Create NuGet package
```

### Run Sample
```bash
cd samples/Kommand.Sample
dotnet run
```

## Key Implementation Notes

### 1. Handler Resolution Pattern

Handlers are resolved **using reflection** from IServiceProvider. The Mediator class builds the handler type dynamically:

```csharp
var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
var handler = _serviceProvider.GetService(handlerType);
```

This is necessary because the exact command type is not known at compile time.

### 2. Interceptor Pipeline Construction

Interceptors are built in **reverse order** (last registered = innermost):

```
User Registration Order: [Logging, Validation, Metrics]
Execution Order:
  → Logging (enter)
    → Validation (enter)
      → Metrics (enter)
        → Handler
      → Metrics (exit)
    → Validation (exit)
  → Logging (exit)
```

The pipeline is built by wrapping delegates recursively.

### 3. Auto-Discovery During Assembly Scanning

`RegisterHandlersFromAssembly()` scans for:
- `ICommandHandler<,>` implementations
- `IQueryHandler<,>` implementations
- `INotificationHandler<>` implementations
- `IValidator<>` implementations (automatic!)

All are registered in DI automatically. Validators are **always Scoped** to support async validation with repository access.

### 4. OpenTelemetry Zero-Config Pattern

OTEL interceptors (`ActivityInterceptor`, `MetricsInterceptor`) are **always registered** but have zero overhead when OTEL is not configured:

```csharp
// Returns NULL if no OTEL configured (~10-50ns overhead)
using var activity = ActivitySource.StartActivity("Command.CreateUser");
activity?.SetTag("kommand.request.type", "Command"); // Null-safe
```

Auto-registration uses `IConfigureOptions<TracerProviderBuilder>` to subscribe to OTEL when user configures it in their app.

### 5. Validation Execution

When `config.WithValidation()` is called:
1. `ValidationInterceptor<,>` is added to interceptor pipeline
2. It injects `IEnumerable<IValidator<TRequest>>` (all validators for that request)
3. Runs ALL validators sequentially
4. Collects ALL errors from all validators
5. Throws `ValidationException` if any validation fails (short-circuits handler)

### 6. Notification Error Handling

Notifications use "continue on failure" strategy:
- All handlers execute even if one fails
- Exceptions are caught and logged (not propagated)
- This ensures domain events are resilient

## Critical Constraints

### DO
- ✅ Use `HandleAsync` (not just `Handle`) for all handler methods
- ✅ Make Mediator class `internal sealed` (not public)
- ✅ Use Scoped lifetime for handlers by default
- ✅ Auto-register OTEL interceptors (unless explicitly disabled)
- ✅ Follow exact folder structure from MEDIATOR_ARCHITECTURE_PLAN.md
- ✅ Target net8.0 (forward compatible with .NET 8, 9, 10+)
- ✅ Use namespace structure that matches folder structure (e.g., `Kommand.Abstractions` for files in `Abstractions/` folder)
- ✅ Core types like `Unit` live in root `Kommand` namespace and folder
- ✅ Include comprehensive XML documentation for all public APIs
- ✅ Use contravariant `in` modifier for handler/validator type parameters
- ✅ Use covariant `out` modifier for response type parameters

### DON'T
- ❌ Add external dependencies beyond the two specified
- ❌ Create multiple NuGet packages (single package only)
- ❌ Use Transient lifetime as default (use Scoped)
- ❌ Expose internal implementation classes (Mediator, ServiceFactory, etc.)
- ❌ Include FluentValidation or any commercial libraries
- ❌ Add Result<T> pattern in v1.0 (deferred to v2.0)
- ❌ Support streaming queries in v1.0 (deferred to v2.0)
- ❌ Make OTEL dependencies hard requirements (they must be optional)

## Testing Requirements

- **Minimum Coverage**: 80% overall
- **Core Components**: 95-100% (Mediator, interceptor pipeline)
- **Test Structure**: Single project (`Kommand.Tests`) with `Unit/` and `Integration/` folders
- **Test Frameworks**: xUnit + NSubstitute (don't use FluentAssertions or any other assertion library. Work with the built in assertions in xUnit)
- **Pattern**: AAA (Arrange-Act-Assert)

## Performance Targets

From Architecture Doc Section 11:

| Scenario | Target Overhead | Notes |
|----------|----------------|-------|
| Direct method call | 1.0x (baseline) | Reference |
| Kommand without interceptors | <1.5x | DI resolution + dispatch |
| Kommand with 3 interceptors | <2.0x | Acceptable for production |
| OTEL when not configured | ~10-50ns | Negligible |

Use BenchmarkDotNet for validation (see `KOMMAND_TASK_LIST_PART2.md` Task 5.4).

## Task Execution Tips

When implementing tasks:

1. **Read Architecture Doc First** - Every task references specific sections/line numbers in `MEDIATOR_ARCHITECTURE_PLAN.md`
2. **Follow Task Order** - Tasks have dependencies; complete in sequence
3. **Use Task Completion Checklists** - Each task has verification checklist
4. **Run Tests After Each Phase** - Verify integration tests pass
5. **Check Coverage** - Maintain >80% throughout development
6. **Update This File** - Update phase status checkboxes as you progress

## Example: Minimal Usage (Target API)

This is what users should be able to write after implementation:

```csharp
// 1. Define command
public record CreateUserCommand(string Email, string Name) : ICommand<User>;

// 2. Define handler
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _repository;

    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User { Email = command.Email, Name = command.Name };
        await _repository.AddAsync(user, ct);
        return user;
    }
}

// 3. Register in DI (Program.cs)
services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
});

// 4. Use in controller
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, HttpContext.RequestAborted);
        return Created($"/users/{user.Id}", user);
    }
}
```

That's it! Auto-discovered, OTEL traces/metrics included, zero boilerplate.

## References

- **Complete Specification**: `MEDIATOR_ARCHITECTURE_PLAN.md` (10,000 words, 17 sections)
- **Task Breakdown**: `KOMMAND_TASK_LIST.md` + `KOMMAND_TASK_LIST_PART2.md` (52 tasks)
- **License**: MIT (see LICENSE file)
- **Target Framework**: net8.0 (LTS, forward compatible)
- **Build SDK**: Any .NET SDK >= 8.0 (e.g., .NET 8, 9, or 10 SDK)
- **Inspiration**: MediatR (with improvements and MIT license)

## Questions to Ask User

When unsure during implementation:

1. **Dependencies** - "Should I add package X?" (Answer: NO unless explicitly listed)
2. **Design Deviation** - "Task suggests pattern X, but I think Y is better" (Answer: Follow task list exactly; it matches architecture doc)
3. **Test Coverage** - "Is X% coverage enough?" (Answer: Must be >80% overall, 95% for core)
4. **Breaking Changes** - "Can I change public API surface?" (Answer: NO for v1.0; follow MEDIATOR_ARCHITECTURE_PLAN.md exactly)

## Current State

**Status**: Phase 1 (Core Foundation) COMPLETED! All 15 tasks finished, 6/6 integration tests passing.

**Completed in Phase 1**:
- ✅ Project structure and configuration
- ✅ Core abstractions (ICommand, IQuery, IMediator, INotification, handlers)
- ✅ Unit type for void commands
- ✅ Internal Mediator implementation with reflection-based handler resolution
- ✅ DI registration system with assembly scanning
- ✅ KommandConfiguration with handler auto-discovery
- ✅ Comprehensive integration tests

**Next Steps**:
1. Begin Phase 2 (Interceptor System) with Task 2.1
2. Implement IInterceptor<TRequest, TResponse> abstraction
3. Build interceptor pipeline with reverse-order execution
4. Continue through Phase 2 sequentially

**Do not skip tasks or reorder** - they have carefully planned dependencies.
