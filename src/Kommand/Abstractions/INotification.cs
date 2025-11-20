namespace Kommand.Abstractions;

/// <summary>
/// Marker interface for notifications (domain events).
/// Notifications can have zero or more handlers and are used for pub/sub patterns.
/// </summary>
/// <remarks>
/// <para>
/// Notifications represent events that have already occurred in the system (past tense).
/// They are used to notify other parts of the application about state changes without
/// creating tight coupling between components.
/// </para>
/// <para>
/// Unlike commands and queries which have exactly one handler, notifications support
/// multiple handlers (0-N). This enables:
/// <list type="bullet">
/// <item><description>Domain event handling across bounded contexts</description></item>
/// <item><description>Cross-cutting concerns (logging, auditing, caching)</description></item>
/// <item><description>Integration events for external systems</description></item>
/// <item><description>Workflow orchestration and sagas</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Naming Convention:</strong> Notifications should be named in past tense to indicate
/// something has already happened, such as UserCreated, OrderPlaced, PaymentProcessed, etc.
/// </para>
/// <para>
/// <strong>Error Handling:</strong> By design, if one notification handler fails, others will
/// still execute. This ensures resilience - one component's failure doesn't cascade to others.
/// Failed handlers will log errors but won't throw exceptions to the caller.
/// </para>
/// <example>
/// Example notification for a user creation event:
/// <code>
/// public record UserCreatedNotification(Guid UserId, string Email, DateTime CreatedAt)
///     : INotification;
/// </code>
/// </example>
/// <example>
/// Example notification for an order placed event with rich data:
/// <code>
/// public record OrderPlacedNotification : INotification
/// {
///     public Guid OrderId { get; init; }
///     public Guid CustomerId { get; init; }
///     public decimal TotalAmount { get; init; }
///     public DateTime PlacedAt { get; init; }
///     public List&lt;OrderItem&gt; Items { get; init; } = new();
/// }
/// </code>
/// </example>
/// </remarks>
public interface INotification
{
}
