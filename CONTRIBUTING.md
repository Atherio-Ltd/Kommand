# Contributing to Kommand

First off, thank you for considering contributing to Kommand! It's people like you that make Kommand such a great tool for the .NET community.

## Code of Conduct

This project adheres to a simple code of conduct: **Be respectful and professional**. We're all here to build something great together.

- Be welcoming to newcomers
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the [existing issues](https://github.com/Atherio-Ltd/Kommand/issues) to avoid duplicates.

When you create a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (code snippets, test cases)
- **Describe the behavior you observed** and what you expected
- **Include details about your environment** (.NET version, OS, etc.)

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md) when creating issues.

### Suggesting Features

Feature suggestions are welcome! Before creating a feature request:

- **Check if the feature already exists** in the latest version
- **Check existing feature requests** to avoid duplicates
- **Consider if it aligns with the project goals** (see [Architecture Document](docs/ARCHITECTURE.md))

When suggesting a feature:

- **Use a clear and descriptive title**
- **Provide a detailed description** of the proposed feature
- **Explain why this feature would be useful** to most users
- **Provide examples** of how it would be used

Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md) when creating requests.

## Development Setup

### Prerequisites

- **.NET 8 SDK or later** (.NET 9, 10+ supported)
- **Git**
- **IDE**: Visual Studio 2022, Rider, or VS Code with C# extension

### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Kommand.git
   cd Kommand
   ```

3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/my-awesome-feature
   # or
   git checkout -b fix/bug-description
   ```

4. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

5. **Build the project**:
   ```bash
   dotnet build
   ```

6. **Run tests**:
   ```bash
   dotnet test
   ```

### Project Structure

```
Kommand/
├── src/Kommand/                    # Main library
│   ├── Abstractions/               # Public interfaces
│   ├── Interceptors/               # Interceptor system
│   ├── Validation/                 # Validation system
│   ├── Implementation/             # Internal implementation
│   └── Registration/               # DI registration
├── tests/
│   ├── Kommand.Tests/              # Unit and integration tests
│   └── Kommand.Benchmarks/         # Performance benchmarks
├── samples/Kommand.Sample/         # Working example
└── docs/                           # Documentation
```

## Pull Request Process

### Before Submitting

1. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```

2. **Verify code coverage** (must be >80% overall):
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover
   ```

3. **Run the sample project**:
   ```bash
   cd samples/Kommand.Sample
   dotnet run
   ```

4. **Build in Release mode**:
   ```bash
   dotnet build -c Release
   ```

### Submitting a Pull Request

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/my-awesome-feature
   ```

2. **Open a Pull Request** on GitHub

3. **Fill out the PR template** completely

4. **Link any related issues** using keywords (e.g., "Fixes #123")

5. **Wait for review** - maintainers will review your PR and may request changes

### PR Review Criteria

Your PR will be reviewed for:

- **Functionality**: Does it work as intended?
- **Tests**: Are there tests covering the changes?
- **Code quality**: Is the code clean and maintainable?
- **Documentation**: Are XML docs and guides updated?
- **Performance**: Does it maintain performance standards?
- **Breaking changes**: Are they necessary and documented?

## Coding Standards

Kommand follows strict coding standards to maintain quality and consistency.

### General Guidelines

1. **Follow the existing code style** - consistency is key
2. **Write clear, self-documenting code** - use descriptive names
3. **Keep methods small and focused** - single responsibility
4. **Avoid premature optimization** - clarity first, optimize when needed
5. **No external dependencies** - except DI abstractions and DiagnosticSource

### C# Coding Conventions

- **Use C# 12 features** where appropriate (file-scoped namespaces, records, etc.)
- **Nullable reference types**: Enabled - handle nulls explicitly
- **Async/await**: All async methods must have `Async` suffix
- **Naming**:
  - PascalCase for public members
  - camelCase for private fields (with `_` prefix)
  - Interfaces start with `I`
- **File organization**:
  - One public type per file
  - File name matches type name
  - Namespace matches folder structure

### Architecture Constraints

**Must follow** (from [Architecture Document](docs/ARCHITECTURE.md)):

✅ **DO:**
- Use `HandleAsync` for all handler methods (not just `Handle`)
- Make internal implementation classes `internal sealed`
- Use Scoped lifetime for handlers by default
- Include comprehensive XML documentation for all public APIs
- Use contravariant `in` modifier for handler type parameters
- Follow existing folder structure

❌ **DON'T:**
- Add external dependencies beyond Microsoft.Extensions.DependencyInjection.Abstractions and System.Diagnostics.DiagnosticSource
- Expose internal implementation classes
- Use Transient lifetime as default
- Create multiple NuGet packages
- Break existing public APIs without major version bump

## Testing Requirements

### Test Coverage

- **Minimum overall coverage**: 80%
- **Core components** (Mediator, interceptors): 95-100%
- **All new features must include tests**

### Test Structure

- Use **xUnit** for test framework
- Use **AAA pattern** (Arrange-Act-Assert)
- **One assertion per test** (when possible)
- **Clear test names** describing what is tested

### Test Example

```csharp
[Fact]
public async Task SendAsync_WithValidCommand_ReturnsExpectedResult()
{
    // Arrange
    var mediator = CreateMediator();
    var command = new TestCommand("test");

    // Act
    var result = await mediator.SendAsync(command, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("expected", result.Value);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~SendAsync_WithValidCommand"

# Run in watch mode
dotnet watch test
```

## Documentation Requirements

All public APIs **must** have XML documentation:

```csharp
/// <summary>
/// Sends a command to its handler and returns the result.
/// </summary>
/// <typeparam name="TResponse">The type of response expected.</typeparam>
/// <param name="command">The command to send.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The response from the command handler.</returns>
/// <exception cref="HandlerNotFoundException">
/// Thrown when no handler is registered for the command type.
/// </exception>
Task<TResponse> SendAsync<TResponse>(
    ICommand<TResponse> command,
    CancellationToken cancellationToken);
```

### Documentation Updates

When adding features, also update:

- **README.md** - If it's a major feature
- **docs/getting-started.md** - If it affects getting started
- **CHANGELOG.md** - Always add entry for next release
- **Sample project** - Add example usage if applicable

## Performance Considerations

Kommand has strict performance targets (see [Architecture Document](docs/ARCHITECTURE.md)):

- **Mediator dispatch overhead**: <2 μs
- **Per-interceptor cost**: <100 ns
- **Total with 3 interceptors**: <3 μs

### Running Benchmarks

```bash
cd tests/Kommand.Benchmarks

# Microbenchmarks (absolute overhead)
dotnet run -c Release

# Realistic workloads (overhead percentage)
dotnet run -c Release -- --realistic
```

If your changes affect performance:

1. Run benchmarks before and after
2. Document any performance changes in the PR
3. If performance regresses, explain why it's necessary

## Commit Message Guidelines

Write clear, descriptive commit messages:

```
feat: Add support for async validation with database access

- Implement IValidator<T> interface
- Add ValidationInterceptor for pipeline integration
- Include 15 unit tests covering edge cases
- Update getting started guide with examples

Closes #123
```

**Format:**
- **Type**: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `chore`
- **Subject**: Brief description (50 chars or less)
- **Body**: Detailed explanation (wrap at 72 chars)
- **Footer**: Reference issues (e.g., "Closes #123")

## Release Process

Kommand uses semantic versioning (SemVer):

- **Major** (1.0.0 → 2.0.0): Breaking changes
- **Minor** (1.0.0 → 1.1.0): New features, backwards compatible
- **Patch** (1.0.0 → 1.0.1): Bug fixes, backwards compatible

Pre-release tags:
- **Alpha**: `1.0.0-alpha.1` - Early testing
- **Beta**: `1.0.0-beta.1` - Feature complete, testing
- **RC**: `1.0.0-rc.1` - Release candidate

## Questions?

- **General questions**: [GitHub Discussions](https://github.com/Atherio-Ltd/Kommand/discussions)
- **Bug reports**: [GitHub Issues](https://github.com/Atherio-Ltd/Kommand/issues)
- **Feature requests**: [GitHub Issues](https://github.com/Atherio-Ltd/Kommand/issues)

## License

By contributing to Kommand, you agree that your contributions will be licensed under the [MIT License](LICENSE).

---

Thank you for contributing to Kommand! 🚀
