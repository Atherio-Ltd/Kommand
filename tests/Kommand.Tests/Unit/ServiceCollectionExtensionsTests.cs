namespace Kommand.Tests.Unit;

using Kommand.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddKommand throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddKommand_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddKommand(config => { }));
    }

    /// <summary>
    /// Verifies that AddKommand throws ArgumentNullException when configuration action is null.
    /// </summary>
    [Fact]
    public void AddKommand_WithNullConfigurationAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddKommand(null!));
    }

    /// <summary>
    /// Verifies that AddKommand registers IMediator as Scoped.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldRegisterMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKommand(config => { });

        // Assert
        var mediatorDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));
        Assert.NotNull(mediatorDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, mediatorDescriptor.Lifetime);
    }

    /// <summary>
    /// Verifies that AddKommand executes the configuration action.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldExecuteConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationActionExecuted = false;

        // Act
        services.AddKommand(config =>
        {
            configurationActionExecuted = true;
        });

        // Assert
        Assert.True(configurationActionExecuted);
    }

    /// <summary>
    /// Verifies that AddKommand registers all handler descriptors from configuration.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldRegisterAllHandlerDescriptorsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ExtensionsTestCommand).Assembly);
        });

        // Assert
        var commandHandler = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<ExtensionsTestCommand, string>));

        Assert.NotNull(commandHandler);
        Assert.Equal(typeof(ExtensionsTestCommandHandler), commandHandler.ImplementationType);
    }

    /// <summary>
    /// Verifies that AddKommand can be called multiple times.
    /// </summary>
    [Fact]
    public void AddKommand_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ExtensionsTestCommand).Assembly);
        });

        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(AnotherExtensionsTestCommand).Assembly);
        });

        // Assert - Should have registered mediator and handlers from both calls
        var mediatorRegistrations = services.Where(d => d.ServiceType == typeof(IMediator)).ToList();
        Assert.Equal(2, mediatorRegistrations.Count); // One from each call

        var commandHandler1 = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<ExtensionsTestCommand, string>));
        var commandHandler2 = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<AnotherExtensionsTestCommand, int>));

        Assert.NotNull(commandHandler1);
        Assert.NotNull(commandHandler2);
    }

    /// <summary>
    /// Verifies that AddKommand returns the service collection for fluent chaining.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldReturnServiceCollectionForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddKommand(config => { });

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    /// Verifies that handlers can be resolved from the built service provider.
    /// </summary>
    [Fact]
    public void AddKommand_RegisteredHandlers_ShouldBeResolvableFromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ExtensionsTestCommand).Assembly);
        });

        // Act
        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<ICommandHandler<ExtensionsTestCommand, string>>();

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<ExtensionsTestCommandHandler>(handler);
    }

    /// <summary>
    /// Verifies that IMediator can be resolved from the built service provider.
    /// </summary>
    [Fact]
    public void AddKommand_ShouldAllowMediatorToBeResolvedFromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config => { });

        // Act
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        // Assert
        Assert.NotNull(mediator);
    }

    /// <summary>
    /// Verifies that scoped handlers get new instances per scope.
    /// </summary>
    [Fact]
    public void AddKommand_ScopedHandlers_ShouldGetNewInstancePerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ExtensionsTestCommand).Assembly, ServiceLifetime.Scoped);
        });

        var provider = services.BuildServiceProvider();

        // Act
        var handler1 = provider.CreateScope().ServiceProvider.GetService<ICommandHandler<ExtensionsTestCommand, string>>();
        var handler2 = provider.CreateScope().ServiceProvider.GetService<ICommandHandler<ExtensionsTestCommand, string>>();

        // Assert - Different instances per scope
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotSame(handler1, handler2);
    }

    /// <summary>
    /// Verifies that singleton handlers get the same instance across scopes.
    /// </summary>
    [Fact]
    public void AddKommand_SingletonHandlers_ShouldGetSameInstanceAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKommand(config =>
        {
            config.RegisterHandlersFromAssembly(typeof(ExtensionsTestCommand).Assembly, ServiceLifetime.Singleton);
        });

        var provider = services.BuildServiceProvider();

        // Act
        var handler1 = provider.CreateScope().ServiceProvider.GetService<ICommandHandler<ExtensionsTestCommand, string>>();
        var handler2 = provider.CreateScope().ServiceProvider.GetService<ICommandHandler<ExtensionsTestCommand, string>>();

        // Assert - Same instance across scopes
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.Same(handler1, handler2);
    }
}

// ============================================================================
// Test Fixtures
// ============================================================================

public record ExtensionsTestCommand : ICommand<string>;
public class ExtensionsTestCommandHandler : ICommandHandler<ExtensionsTestCommand, string>
{
    public Task<string> HandleAsync(ExtensionsTestCommand command, CancellationToken cancellationToken)
        => Task.FromResult("test");
}

public record AnotherExtensionsTestCommand : ICommand<int>;
public class AnotherExtensionsTestCommandHandler : ICommandHandler<AnotherExtensionsTestCommand, int>
{
    public Task<int> HandleAsync(AnotherExtensionsTestCommand command, CancellationToken cancellationToken)
        => Task.FromResult(42);
}
