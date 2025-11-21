namespace Kommand.Tests.Unit;

using System.Reflection;
using Kommand.Abstractions;
using Kommand.Registration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for KommandConfiguration.
/// </summary>
public class KommandConfigurationTests
{
    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly throws ArgumentNullException when assembly is null.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_WithNullAssembly_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            config.RegisterHandlersFromAssembly(null!));
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly uses Scoped lifetime by default.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_WithoutLifetimeParameter_ShouldUseScopedLifetime()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestCommand).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var commandHandlerDescriptor = config.HandlerDescriptors
            .First(d => d.ServiceType == typeof(ICommandHandler<ConfigTestCommand, string>));

        Assert.Equal(ServiceLifetime.Scoped, commandHandlerDescriptor.Lifetime);
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly respects custom lifetime parameter.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_WithCustomLifetime_ShouldUseSpecifiedLifetime()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestCommand).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly, ServiceLifetime.Singleton);

        // Assert
        var commandHandlerDescriptor = config.HandlerDescriptors
            .First(d => d.ServiceType == typeof(ICommandHandler<ConfigTestCommand, string>));

        Assert.Equal(ServiceLifetime.Singleton, commandHandlerDescriptor.Lifetime);
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly discovers command handlers.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_ShouldDiscoverCommandHandlers()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestCommand).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var commandHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ServiceType == typeof(ICommandHandler<ConfigTestCommand, string>));

        Assert.NotNull(commandHandler);
        Assert.Equal(typeof(ConfigTestCommandHandler), commandHandler.ImplementationType);
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly discovers query handlers.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_ShouldDiscoverQueryHandlers()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestQuery).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var queryHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ServiceType == typeof(IQueryHandler<ConfigTestQuery, int>));

        Assert.NotNull(queryHandler);
        Assert.Equal(typeof(ConfigTestQueryHandler), queryHandler.ImplementationType);
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly discovers notification handlers.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_ShouldDiscoverNotificationHandlers()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestNotification).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var notificationHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ServiceType == typeof(INotificationHandler<ConfigTestNotification>));

        Assert.NotNull(notificationHandler);
        Assert.Equal(typeof(ConfigTestNotificationHandler), notificationHandler.ImplementationType);
    }

    /// <summary>
    /// Verifies that a single class implementing multiple handler interfaces
    /// gets registered for all interfaces.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_WithMultipleInterfacesOnOneClass_ShouldRegisterAll()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(MultiInterfaceHandler).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var commandHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ServiceType == typeof(ICommandHandler<MultiCommand, string>));
        var queryHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ServiceType == typeof(IQueryHandler<MultiQuery, string>));

        Assert.NotNull(commandHandler);
        Assert.NotNull(queryHandler);
        Assert.Equal(typeof(MultiInterfaceHandler), commandHandler.ImplementationType);
        Assert.Equal(typeof(MultiInterfaceHandler), queryHandler.ImplementationType);
    }

    /// <summary>
    /// Verifies that abstract classes are not registered as handlers.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_ShouldNotRegisterAbstractClasses()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(AbstractCommandHandler).Assembly;

        // Act
        config.RegisterHandlersFromAssembly(assembly);

        // Assert
        var abstractHandler = config.HandlerDescriptors
            .FirstOrDefault(d => d.ImplementationType == typeof(AbstractCommandHandler));

        Assert.Null(abstractHandler);
    }

    /// <summary>
    /// Verifies that AddInterceptor throws ArgumentNullException when type is null.
    /// </summary>
    [Fact]
    public void AddInterceptor_WithNullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            config.AddInterceptor(null!));
    }

    /// <summary>
    /// Verifies that AddInterceptor adds the type to InterceptorTypes collection.
    /// </summary>
    [Fact]
    public void AddInterceptor_ShouldAddTypeToCollection()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act
        config.AddInterceptor(typeof(DummyInterceptor));

        // Assert
        Assert.Contains(typeof(DummyInterceptor), config.InterceptorTypes);
    }

    /// <summary>
    /// Verifies that AddInterceptor generic overload works correctly.
    /// </summary>
    [Fact]
    public void AddInterceptor_GenericOverload_ShouldAddTypeToCollection()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act
        config.AddInterceptor<DummyInterceptor>();

        // Assert
        Assert.Contains(typeof(DummyInterceptor), config.InterceptorTypes);
    }

    /// <summary>
    /// Verifies that AddInterceptor maintains order of interceptors.
    /// </summary>
    [Fact]
    public void AddInterceptor_ShouldMaintainOrderOfInterceptors()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act
        config.AddInterceptor<DummyInterceptor>();
        config.AddInterceptor<AnotherDummyInterceptor>();

        // Assert
        Assert.Equal(2, config.InterceptorTypes.Count);
        Assert.Equal(typeof(DummyInterceptor), config.InterceptorTypes[0]);
        Assert.Equal(typeof(AnotherDummyInterceptor), config.InterceptorTypes[1]);
    }

    /// <summary>
    /// Verifies that WithValidation returns the configuration instance for fluent chaining.
    /// </summary>
    [Fact]
    public void WithValidation_ShouldReturnConfigurationForFluentChaining()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act
        var result = config.WithValidation();

        // Assert
        Assert.Same(config, result);
    }

    /// <summary>
    /// Verifies that WithValidation doesn't throw (placeholder behavior for Phase 4).
    /// </summary>
    [Fact]
    public void WithValidation_ShouldNotThrow()
    {
        // Arrange
        var config = new KommandConfiguration();

        // Act & Assert - Should not throw
        config.WithValidation();
    }

    /// <summary>
    /// Verifies that RegisterHandlersFromAssembly returns configuration for fluent chaining.
    /// </summary>
    [Fact]
    public void RegisterHandlersFromAssembly_ShouldReturnConfigurationForFluentChaining()
    {
        // Arrange
        var config = new KommandConfiguration();
        var assembly = typeof(ConfigTestCommand).Assembly;

        // Act
        var result = config.RegisterHandlersFromAssembly(assembly);

        // Assert
        Assert.Same(config, result);
    }

    /// <summary>
    /// Verifies that DefaultHandlerLifetime property defaults to Scoped.
    /// </summary>
    [Fact]
    public void DefaultHandlerLifetime_ShouldDefaultToScoped()
    {
        // Arrange & Act
        var config = new KommandConfiguration();

        // Assert
        Assert.Equal(ServiceLifetime.Scoped, config.DefaultHandlerLifetime);
    }

    /// <summary>
    /// Verifies that DefaultHandlerLifetime can be changed.
    /// </summary>
    [Fact]
    public void DefaultHandlerLifetime_CanBeChanged()
    {
        // Arrange
        var config = new KommandConfiguration
        {
            DefaultHandlerLifetime = ServiceLifetime.Transient
        };

        // Assert
        Assert.Equal(ServiceLifetime.Transient, config.DefaultHandlerLifetime);
    }
}

// ============================================================================
// Test Fixtures
// ============================================================================

public record ConfigTestCommand : ICommand<string>;
public class ConfigTestCommandHandler : ICommandHandler<ConfigTestCommand, string>
{
    public Task<string> HandleAsync(ConfigTestCommand command, CancellationToken cancellationToken)
        => Task.FromResult("test");
}

public record ConfigTestQuery : IQuery<int>;
public class ConfigTestQueryHandler : IQueryHandler<ConfigTestQuery, int>
{
    public Task<int> HandleAsync(ConfigTestQuery query, CancellationToken cancellationToken)
        => Task.FromResult(42);
}

public record ConfigTestNotification : INotification;
public class ConfigTestNotificationHandler : INotificationHandler<ConfigTestNotification>
{
    public Task HandleAsync(ConfigTestNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public record MultiCommand : ICommand<string>;
public record MultiQuery : IQuery<string>;

public class MultiInterfaceHandler :
    ICommandHandler<MultiCommand, string>,
    IQueryHandler<MultiQuery, string>
{
    public Task<string> HandleAsync(MultiCommand command, CancellationToken cancellationToken)
        => Task.FromResult("command");

    public Task<string> HandleAsync(MultiQuery query, CancellationToken cancellationToken)
        => Task.FromResult("query");
}

public record AbstractCommand : ICommand<string>;
public abstract class AbstractCommandHandler : ICommandHandler<AbstractCommand, string>
{
    public abstract Task<string> HandleAsync(AbstractCommand command, CancellationToken cancellationToken);
}

public class DummyInterceptor { }
public class AnotherDummyInterceptor { }
