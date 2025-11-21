---
name: Feature Request
about: Suggest a new feature or enhancement
title: '[FEATURE] '
labels: enhancement
assignees: ''
---

## Feature Description

A clear and concise description of the feature you'd like to see.

## Problem Statement

What problem does this feature solve? Why is it needed?

**Example**: "I'm always frustrated when..."

## Proposed Solution

Describe how you envision this feature working.

## API Design (if applicable)

```csharp
// Show how the API would look
public interface IMyNewFeature
{
    Task DoSomethingAsync(CancellationToken ct);
}
```

## Usage Example

```csharp
// Show how users would use this feature
builder.Services.AddKommand(config =>
{
    config.WithMyNewFeature(); // Example
});
```

## Alternatives Considered

Have you considered any alternative solutions or features?

## Benefits

Who would benefit from this feature?

- [ ] All Kommand users
- [ ] Users with specific scenarios (describe below)
- [ ] Advanced users only
- [ ] Improves performance
- [ ] Improves developer experience
- [ ] Adds new capability

## Alignment with Project Goals

Does this feature align with Kommand's goals?

- [ ] Zero external dependencies maintained
- [ ] Performance not significantly impacted
- [ ] Simple API (easy to use)
- [ ] Works well with existing features
- [ ] Follows CQRS principles

## Additional Context

Add any other context, screenshots, or examples about the feature request here.

## Breaking Changes

Would this feature require breaking changes to the existing API?

- [ ] No breaking changes
- [ ] Minor breaking changes (can be mitigated)
- [ ] Major breaking changes (would require major version bump)

## Implementation Willingness

Are you willing to contribute to implementing this feature?

- [ ] Yes, I can submit a PR
- [ ] Yes, with guidance
- [ ] No, but I can help test
- [ ] No, just suggesting
