---
name: Bug Report
about: Report a bug or unexpected behavior
title: '[BUG] '
labels: bug
assignees: ''
---

## Description

A clear and concise description of the bug.

## Steps to Reproduce

1. Create a command/handler with...
2. Register with DI using...
3. Execute with...
4. Observe error...

## Expected Behavior

What you expected to happen.

## Actual Behavior

What actually happened.

## Code Sample

```csharp
// Minimal reproducible example
public record MyCommand : ICommand<string>;

public class MyCommandHandler : ICommandHandler<MyCommand, string>
{
    public async Task<string> HandleAsync(MyCommand command, CancellationToken ct)
    {
        // Your code here
    }
}
```

## Environment

- **Kommand Version**: [e.g., 1.0.0-alpha.1]
- **.NET Version**: [e.g., .NET 8.0, .NET 9.0]
- **OS**: [e.g., Windows 11, Ubuntu 22.04, macOS 14]
- **IDE**: [e.g., Visual Studio 2022, Rider, VS Code]

## Stack Trace (if applicable)

```
Paste stack trace here
```

## Additional Context

Add any other context about the problem here (screenshots, logs, etc.).

## Possible Solution (optional)

If you have ideas on how to fix this, please share!
