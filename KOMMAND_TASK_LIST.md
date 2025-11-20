# Kommand Implementation Task List

## Project Overview
This document contains a granular task breakdown for implementing the Kommand library - a production-ready CQRS mediator for .NET 9. Each task is designed to be completed by a mid-level engineer in approximately 1 hour.

**Important**: This is for a **standalone repository** called `kommand/`, separate from the Atherio monorepo.

**Overall Progress**: 14/52 tasks completed (26.9%)

**Reference**: All tasks align with `MEDIATOR_ARCHITECTURE_PLAN.md` sections.

---

## Phase 1: Core Foundation (15 tasks)

### Task 1.1: Create Repository and Project Structure
**Status**: [x] Completed

**Objective**: Set up a new Git repository with the .NET 9 class library project structure (Architecture Doc Section 5).

**Instructions**:
1. Create new directory for the standalone repo: `kommand/`
2. Initialize Git repository: `git init`
3. Create `.gitignore` for .NET projects
4. Create directory structure at root:
   - `src/`
   - `tests/`
   - `samples/`
   - `docs/`
5. Create .NET class library inside `src/`:
   ```bash
   cd src
   dotnet new classlib -n Kommand -f net8.0
   ```
6. Create solution file at root:
   ```bash
   cd ..
   dotnet new sln -n kommand
   dotnet sln add src/Kommand/Kommand.csproj
   ```
7. Configure `Kommand.csproj` properties:
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0</TargetFramework>
     <Nullable>enable</Nullable>
     <ImplicitUsings>enable</ImplicitUsings>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
     <RootNamespace>Kommand</RootNamespace>
   </PropertyGroup>
   ```
   Note: Single target net8.0 (forward compatible with .NET 9, 10+)
8. Create MIT LICENSE file at root
9. Delete default `Class1.cs` file

**How it fits**: This is the foundation - all code will be added to this structure.

**Builds upon**: Nothing (first task)

**Future dependencies**: All subsequent tasks depend on this structure.

**Completion Checklist**:
- [ ] `kommand/` directory is a Git repository
- [ ] Directory structure matches: `src/`, `tests/`, `samples/`, `docs/` at root
- [ ] `src/Kommand/Kommand.csproj` exists and builds successfully
- [ ] Solution file `kommand.sln` at root
- [ ] Nullable enabled, warnings as errors
- [ ] MIT LICENSE file exists
- [ ] Project builds: `dotnet build` succeeds

---

### Task 1.2: Create Folder Structure Inside Kommand Project
**Status**: [x] Completed

**Objective**: Create the internal folder structure for organizing code (Architecture Doc Section 5).

**Instructions**:
1. Inside `src/Kommand/`, create directories:
   - `Abstractions/`
   - `Interceptors/`
   - `Validation/`
   - `Implementation/`
   - `Registration/`
   - `Exceptions/`
2. Verify structure matches Architecture Doc Section 5 exactly
3. Do NOT create any files yet

**How it fits**: Organizes code into logical components per the architecture.

**Builds upon**: Task 1.1 (project structure)

**Future dependencies**: All implementation tasks will place files in these directories.

**Completion Checklist**:
- [ ] All 6 directories created inside `src/Kommand/`
- [ ] Directory names match architecture document exactly
- [ ] No files created yet (just folders)

---

### Task 1.3: Implement Core Request Abstractions
**Status**: [x] Completed

**Objective**: Create the foundational marker interfaces for requests (Architecture Doc Section 2, lines 75-86).

**Instructions**:
1. In `src/Kommand/Abstractions/`, create `IRequest.cs`:
   ```csharp
   namespace Kommand.Abstractions;

   using Kommand;

   /// <summary>
   /// Marker interface for all requests with a response type.
   /// </summary>
   /// <typeparam name="TResponse">The response type</typeparam>
   public interface IRequest<out TResponse> { }

   /// <summary>
   /// Marker interface for requests without a response (void).
   /// </summary>
   public interface IRequest : IRequest<Unit> { }
   ```
2. In `src/Kommand/` (root folder), create `Unit.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Represents a void return type for requests that don't return data.
   /// </summary>
   public readonly struct Unit
   {
       public static readonly Unit Value = default;
   }
   ```
3. Add comprehensive XML documentation explaining:
   - What IRequest represents
   - When to use IRequest<TResponse> vs IRequest
   - What Unit is for (void equivalent)
   - Include usage examples in XML comments

**How it fits**: These are the base abstractions that all commands, queries, and notifications build upon.

**Builds upon**: Task 1.2 (folder structure)

**Future dependencies**:
- Task 1.4 (ICommand/IQuery inherit from these)
- Task 1.5 (handlers use these)
- All user code will implement these

**Completion Checklist**:
- [ ] `IRequest<TResponse>` with covariant `out` modifier
- [ ] `IRequest` inherits from `IRequest<Unit>`
- [ ] `Unit` struct with static `Value` field
- [ ] IRequest types in `Kommand.Abstractions` namespace (matching folder structure)
- [ ] Unit type in root `Kommand` namespace (root folder)
- [ ] IRequest.cs includes `using Kommand;` to reference Unit
- [ ] Comprehensive XML documentation
- [ ] Project builds with zero warnings

---

### Task 1.4: Implement Command and Query Abstractions
**Status**: [x] Completed

**Objective**: Create CQRS-specific interfaces for commands and queries (Architecture Doc Section 2, lines 83-86).

**Instructions**:
1. In `src/Kommand/Abstractions/`, create `ICommand.cs`:
   ```csharp
   namespace Kommand.Abstractions;

   using Kommand;

   /// <summary>
   /// Marker interface for commands (write operations that change state).
   /// </summary>
   /// <typeparam name="TResponse">The response type</typeparam>
   public interface ICommand<out TResponse> : IRequest<TResponse> { }

   /// <summary>
   /// Marker interface for commands without a response (void commands).
   /// </summary>
   public interface ICommand : ICommand<Unit> { }
   ```
2. Create `IQuery.cs` in same directory:
   ```csharp
   namespace Kommand.Abstractions;

   /// <summary>
   /// Marker interface for queries (read-only operations that don't change state).
   /// </summary>
   /// <typeparam name="TResponse">The response type</typeparam>
   public interface IQuery<out TResponse> : IRequest<TResponse> { }
   ```
3. Add XML documentation explaining:
   - Semantic difference between commands (write) and queries (read)
   - When to use each
   - CQRS principles
   - Example implementations

**How it fits**: Enables CQRS pattern with explicit semantic distinction between read and write operations.

**Builds upon**: Task 1.3 (IRequest interfaces)

**Future dependencies**:
- Task 1.5 (handlers for commands and queries)
- Task 1.8 (IMediator methods)
- All user code will implement these

**Completion Checklist**:
- [ ] `ICommand<TResponse>` inherits from `IRequest<TResponse>`
- [ ] `ICommand` inherits from `ICommand<Unit>`
- [ ] `IQuery<TResponse>` inherits from `IRequest<TResponse>`
- [ ] Covariant `out` modifiers used correctly
- [ ] XML documentation explains CQRS semantics
- [ ] Example usage in XML comments
- [ ] Project builds successfully

---

### Task 1.5: Implement Handler Abstractions
**Status**: [x] Completed

**Objective**: Create handler interfaces for processing commands and queries (Architecture Doc Section 2, lines 89-99).

**Instructions**:
1. In `src/Kommand/Abstractions/`, create `ICommandHandler.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Handler for processing commands.
   /// </summary>
   /// <typeparam name="TCommand">The command type</typeparam>
   /// <typeparam name="TResponse">The response type</typeparam>
   public interface ICommandHandler<in TCommand, TResponse>
       where TCommand : ICommand<TResponse>
   {
       /// <summary>
       /// Handles the command asynchronously.
       /// </summary>
       /// <param name="command">The command to handle</param>
       /// <param name="cancellationToken">Cancellation token</param>
       /// <returns>The response</returns>
       Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
   }
   ```
2. Create `IQueryHandler.cs` with same pattern:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Handler for processing queries.
   /// </summary>
   /// <typeparam name="TQuery">The query type</typeparam>
   /// <typeparam name="TResponse">The response type</typeparam>
   public interface IQueryHandler<in TQuery, TResponse>
       where TQuery : IQuery<TResponse>
   {
       /// <summary>
       /// Handles the query asynchronously.
       /// </summary>
       /// <param name="query">The query to handle</param>
       /// <param name="cancellationToken">Cancellation token</param>
       /// <returns>The response</returns>
       Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
   }
   ```
3. Add XML documentation explaining:
   - Handler responsibility (business logic execution)
   - That handlers are registered as Scoped by default
   - That cancellation should be respected
   - Example handler implementation

**How it fits**: Handlers contain business logic. The mediator resolves and invokes these.

**Builds upon**: Task 1.4 (ICommand/IQuery interfaces)

**Future dependencies**:
- Task 1.6 (DI registration scans for these)
- Task 1.9 (Mediator invokes these)
- All user code will implement these

**Completion Checklist**:
- [ ] `ICommandHandler<TCommand, TResponse>` interface created
- [ ] `IQueryHandler<TQuery, TResponse>` interface created
- [ ] Generic constraints properly applied
- [ ] Method named `HandleAsync` (not just `Handle`)
- [ ] `CancellationToken` parameter included
- [ ] Return type is `Task<TResponse>`
- [ ] Comprehensive XML documentation
- [ ] Project builds successfully

---

### Task 1.6: Implement Notification Abstractions
**Status**: [x] Completed

**Objective**: Create pub/sub interfaces for domain events (Architecture Doc Section 2, lines 101-108).

**Instructions**:
1. In `src/Kommand/Abstractions/`, create `INotification.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Marker interface for notifications (domain events).
   /// Notifications can have zero or more handlers.
   /// </summary>
   public interface INotification { }
   ```
2. Create `INotificationHandler.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Handler for processing notifications.
   /// Multiple handlers can subscribe to the same notification.
   /// </summary>
   /// <typeparam name="TNotification">The notification type</typeparam>
   public interface INotificationHandler<in TNotification>
       where TNotification : INotification
   {
       /// <summary>
       /// Handles the notification asynchronously.
       /// </summary>
       /// <param name="notification">The notification to handle</param>
       /// <param name="cancellationToken">Cancellation token</param>
       Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
   }
   ```
3. Add XML documentation explaining:
   - Pub/sub pattern
   - Zero or more handlers can subscribe
   - Use cases (domain events, integration events)
   - One handler failure doesn't affect others
   - Execution order not guaranteed

**How it fits**: Enables domain events and pub/sub for loosely coupled components.

**Builds upon**: Task 1.3 (pattern similar to IRequest)

**Future dependencies**:
- Task 1.10 (IMediator.PublishAsync)
- Task 3.3 (notification handler registration)

**Completion Checklist**:
- [ ] `INotification` marker interface created
- [ ] `INotificationHandler<TNotification>` interface created
- [ ] Generic constraint applied
- [ ] Method named `HandleAsync`
- [ ] XML documentation explains pub/sub semantics
- [ ] Documents error handling strategy
- [ ] Project builds successfully

---

### Task 1.7: Implement IMediator Interface
**Status**: [x] Completed

**Objective**: Create the main mediator interface that users will inject (Architecture Doc Section 8, lines 954-978).

**Instructions**:
1. In `src/Kommand/Abstractions/`, create `IMediator.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Mediator for dispatching commands, queries, and notifications.
   /// </summary>
   public interface IMediator
   {
       /// <summary>
       /// Sends a command with a response.
       /// </summary>
       Task<TResponse> SendAsync<TResponse>(
           ICommand<TResponse> command,
           CancellationToken cancellationToken = default);

       /// <summary>
       /// Sends a command without a response (void).
       /// </summary>
       Task SendAsync(
           ICommand command,
           CancellationToken cancellationToken = default);

       /// <summary>
       /// Executes a query and returns the result.
       /// </summary>
       Task<TResponse> QueryAsync<TResponse>(
           IQuery<TResponse> query,
           CancellationToken cancellationToken = default);

       /// <summary>
       /// Publishes a notification to all registered handlers.
       /// </summary>
       Task PublishAsync<TNotification>(
           TNotification notification,
           CancellationToken cancellationToken = default)
           where TNotification : INotification;
   }
   ```
2. Add comprehensive XML documentation:
   - Explain semantic difference between Send vs Query
   - Explain when to use each method
   - Document that Send/Query throw if no handler found
   - Document that Publish is fire-and-forget (no exception if no handlers)
   - Provide usage examples for each method
   - Explain that this is the main entry point for all requests

**How it fits**: This is the main API surface. Users inject IMediator to dispatch all requests.

**Builds upon**:
- Task 1.4 (ICommand/IQuery)
- Task 1.6 (INotification)

**Future dependencies**:
- Task 1.9-1.11 (Mediator implementation)
- All user code will depend on this interface

**Completion Checklist**:
- [ ] Interface named `IMediator`
- [ ] `SendAsync<TResponse>(ICommand<TResponse>)` method
- [ ] `SendAsync(ICommand)` method for void commands
- [ ] `QueryAsync<TResponse>(IQuery<TResponse>)` method
- [ ] `PublishAsync<TNotification>(TNotification)` method
- [ ] All methods have `CancellationToken` with default value
- [ ] Generic constraints properly applied
- [ ] Comprehensive XML documentation with examples
- [ ] Project builds successfully

---

### Task 1.8: Add NuGet Dependencies
**Status**: [x] Completed

**Objective**: Add required NuGet packages (Architecture Doc Section 5, lines 643-649).

**Instructions**:
1. Add NuGet packages to `src/Kommand/Kommand.csproj`:
   ```bash
   cd src/Kommand
   dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.0
   dotnet add package System.Diagnostics.DiagnosticSource --version 8.0.0
   ```
2. Verify these are the ONLY two dependencies
3. These packages provide:
   - DI abstractions (IServiceProvider, IServiceCollection, ServiceDescriptor)
   - OpenTelemetry primitives (ActivitySource, Meter)
4. Do NOT add OpenTelemetry.* packages (those are app-level dependencies)

**How it fits**: Provides necessary abstractions without heavy dependencies.

**Builds upon**: Task 1.1 (project structure)

**Future dependencies**:
- Task 1.12 (DI registration)
- Task 4.1 (OTEL interceptors)

**Completion Checklist**:
- [ ] `Microsoft.Extensions.DependencyInjection.Abstractions` version 8.0.0 added
- [ ] `System.Diagnostics.DiagnosticSource` version 8.0.0 added
- [ ] ONLY these two packages in dependencies
- [ ] Project builds successfully for net8.0 target
- [ ] Can reference IServiceCollection, ActivitySource, Meter types

---

### Task 1.9: Implement Mediator Class - Part 1 (Constructor and SendAsync with Response)
**Status**: [x] Completed

**Objective**: Create Mediator implementation skeleton and SendAsync method (Architecture Doc Section 8).

**Instructions**:
1. In `src/Kommand/Implementation/`, create `Mediator.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Default implementation of IMediator.
   /// </summary>
   internal sealed class Mediator : IMediator
   {
       private readonly IServiceProvider _serviceProvider;

       public Mediator(IServiceProvider serviceProvider)
       {
           _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
       }

       public Task<TResponse> SendAsync<TResponse>(
           ICommand<TResponse> command,
           CancellationToken cancellationToken = default)
       {
           if (command == null) throw new ArgumentNullException(nameof(command));

           // Build handler type
           var commandType = command.GetType();
           var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));

           // Resolve handler
           var handler = _serviceProvider.GetService(handlerType);
           if (handler == null)
           {
               throw new InvalidOperationException(
                   $"No handler registered for command {commandType.Name}");
           }

           // Invoke handler using reflection
           var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync));
           var task = (Task<TResponse>)handleMethod!.Invoke(handler, new object[] { command, cancellationToken })!;
           return task;
       }

       // Stub other methods (throw NotImplementedException for now)
       public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
       {
           throw new NotImplementedException("Implement in Task 1.10");
       }

       public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
       {
           throw new NotImplementedException("Implement in Task 1.10");
       }

       public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
           where TNotification : INotification
       {
           throw new NotImplementedException("Implement in Task 1.11");
       }
   }
   ```
2. Mark class as `internal sealed` (not exposed outside library)
3. Add XML documentation explaining why IServiceProvider is needed

**How it fits**: Core dispatcher that routes commands to handlers.

**Builds upon**:
- Task 1.7 (IMediator interface)
- Task 1.5 (ICommandHandler)
- Task 1.8 (IServiceProvider dependency)

**Future dependencies**:
- Task 1.10 (complete other methods)
- Task 2.4 (add interceptor execution)

**Completion Checklist**:
- [ ] `Mediator` class in `Implementation/` folder
- [ ] Class is `internal sealed`
- [ ] Implements `IMediator`
- [ ] Constructor accepts IServiceProvider
- [ ] Null check on constructor parameter
- [ ] `SendAsync<TResponse>` fully implemented
- [ ] Builds correct generic handler type
- [ ] Resolves handler from service provider
- [ ] Throws descriptive exception if handler not found
- [ ] Invokes HandleAsync using reflection
- [ ] Other methods throw NotImplementedException
- [ ] Project builds successfully

---

### Task 1.10: Implement Mediator Class - Part 2 (SendAsync void and QueryAsync)
**Status**: [x] Completed

**Objective**: Implement remaining dispatch methods for void commands and queries.

**Instructions**:
1. In `src/Kommand/Implementation/Mediator.cs`, implement `SendAsync(ICommand)`:
   ```csharp
   public async Task SendAsync(
       ICommand command,
       CancellationToken cancellationToken = default)
   {
       if (command == null) throw new ArgumentNullException(nameof(command));

       // Build handler type (void commands use Unit as response)
       var commandType = command.GetType();
       var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(Unit));

       // Resolve handler
       var handler = _serviceProvider.GetService(handlerType);
       if (handler == null)
       {
           throw new InvalidOperationException(
               $"No handler registered for command {commandType.Name}");
       }

       // Invoke handler
       var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand, Unit>.HandleAsync));
       await (Task<Unit>)handleMethod!.Invoke(handler, new object[] { command, cancellationToken })!;
   }
   ```
2. Implement `QueryAsync<TResponse>`:
   - Mirror the pattern from SendAsync<TResponse>
   - Use `IQueryHandler<,>` instead of `ICommandHandler<,>`
   - Same error handling and reflection approach
3. Add XML documentation to each method

**How it fits**: Completes command and query dispatching in the mediator.

**Builds upon**: Task 1.9 (SendAsync<TResponse> pattern)

**Future dependencies**: Task 1.11 (PublishAsync)

**Completion Checklist**:
- [ ] `SendAsync(ICommand)` method fully implemented
- [ ] Uses `Unit` as response type for void commands
- [ ] Throws exception if handler not found
- [ ] `QueryAsync<TResponse>` method fully implemented
- [ ] Uses `IQueryHandler<,>` for resolution
- [ ] Both methods have null checks
- [ ] XML documentation added
- [ ] Project builds successfully

---

### Task 1.11: Implement Mediator Class - Part 3 (PublishAsync)
**Status**: [x] Completed

**Objective**: Implement notification publishing (Architecture Doc Section 7, lines 900-926).

**Instructions**:
1. In `src/Kommand/Implementation/Mediator.cs`, implement `PublishAsync`:
   ```csharp
   public async Task PublishAsync<TNotification>(
       TNotification notification,
       CancellationToken cancellationToken = default)
       where TNotification : INotification
   {
       if (notification == null) throw new ArgumentNullException(nameof(notification));

       // Build handler type
       var notificationType = notification.GetType();
       var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

       // Resolve ALL handlers (GetServices returns IEnumerable)
       var handlers = _serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType))
           as IEnumerable<object>;

       if (handlers == null || !handlers.Any())
       {
           // No handlers - this is OK for notifications
           return;
       }

       // Execute handlers sequentially
       var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.HandleAsync));

       foreach (var handler in handlers)
       {
           try
           {
               await (Task)handleMethod!.Invoke(handler, new object[] { notification, cancellationToken })!;
           }
           catch (Exception)
           {
               // Swallow exceptions - one handler failure shouldn't break others
               // TODO: Add logging in future task
           }
       }
   }
   ```
2. Key behaviors per Architecture Doc Section 7:
   - No exception if zero handlers
   - Execute handlers sequentially (not parallel)
   - Continue on failure (one handler exception doesn't stop others)
3. Add XML documentation

**How it fits**: Enables pub/sub for domain events.

**Builds upon**:
- Task 1.10 (dispatch pattern)
- Task 1.6 (INotification interfaces)

**Future dependencies**: Task 3.3 (notification handler registration)

**Completion Checklist**:
- [ ] `PublishAsync` method fully implemented
- [ ] Resolves ALL handlers (IEnumerable)
- [ ] No exception if zero handlers
- [ ] Executes handlers sequentially
- [ ] Catches exceptions from individual handlers
- [ ] Continues execution if one handler fails
- [ ] XML documentation added
- [ ] Project builds successfully

---

### Task 1.12: Implement KommandConfiguration Class - Part 1 (Core Structure)
**Status**: [x] Completed

**Objective**: Create configuration builder class skeleton (Architecture Doc Section 4, lines 344-351).

**Instructions**:
1. In `src/Kommand/Registration/`, create `KommandConfiguration.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Configuration builder for Kommand registration.
   /// </summary>
   public class KommandConfiguration
   {
       private readonly List<ServiceDescriptor> _handlerDescriptors = new();
       private readonly List<ServiceDescriptor> _validatorDescriptors = new();
       private readonly List<Type> _interceptorTypes = new();

       /// <summary>
       /// Default lifetime for auto-discovered handlers (default: Scoped).
       /// </summary>
       public ServiceLifetime DefaultHandlerLifetime { get; set; } = ServiceLifetime.Scoped;

       /// <summary>
       /// Gets registered handler descriptors.
       /// </summary>
       internal IReadOnlyList<ServiceDescriptor> HandlerDescriptors => _handlerDescriptors;

       /// <summary>
       /// Gets registered validator descriptors.
       /// </summary>
       internal IReadOnlyList<ServiceDescriptor> ValidatorDescriptors => _validatorDescriptors;

       /// <summary>
       /// Gets registered interceptor types.
       /// </summary>
       internal IReadOnlyList<Type> InterceptorTypes => _interceptorTypes;

       // Methods will be added in subsequent tasks
   }
   ```
2. Note: This is just the skeleton. Methods will be added in later tasks.
3. Add XML documentation

**How it fits**: Holds all registration configuration before applying to IServiceCollection.

**Builds upon**: Task 1.8 (ServiceDescriptor from DI package)

**Future dependencies**:
- Task 1.13 (add registration methods)
- Task 1.14 (ServiceCollectionExtensions uses this)

**Completion Checklist**:
- [ ] `KommandConfiguration` class in `Registration/` folder
- [ ] Class is public
- [ ] Three private list fields
- [ ] `DefaultHandlerLifetime` property with default Scoped
- [ ] Three internal readonly properties exposing lists
- [ ] XML documentation complete
- [ ] Project builds successfully

---

### Task 1.13: Implement KommandConfiguration Class - Part 2 (Registration Methods)
**Status**: [x] Completed

**Objective**: Add handler/validator registration methods (Architecture Doc Section 4, lines 354-482).

**Instructions**:
1. In `src/Kommand/Registration/KommandConfiguration.cs`, add assembly scanning method:
   ```csharp
   /// <summary>
   /// Registers all handlers and validators from the specified assembly.
   /// </summary>
   public KommandConfiguration RegisterHandlersFromAssembly(
       Assembly assembly,
       ServiceLifetime? lifetime = null)
   {
       if (assembly == null) throw new ArgumentNullException(nameof(assembly));

       var handlerLifetime = lifetime ?? DefaultHandlerLifetime;

       // Scan for command handlers
       var commandHandlers = assembly.GetTypes()
           .Where(t => t.IsClass && !t.IsAbstract)
           .Where(t => t.GetInterfaces().Any(i =>
               i.IsGenericType &&
               i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
           .ToList();

       foreach (var handlerType in commandHandlers)
       {
           var interfaces = handlerType.GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

           foreach (var @interface in interfaces)
           {
               _handlerDescriptors.Add(new ServiceDescriptor(@interface, handlerType, handlerLifetime));
           }
       }

       // Scan for query handlers (same pattern)
       var queryHandlers = assembly.GetTypes()
           .Where(t => t.IsClass && !t.IsAbstract)
           .Where(t => t.GetInterfaces().Any(i =>
               i.IsGenericType &&
               i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
           .ToList();

       foreach (var handlerType in queryHandlers)
       {
           var interfaces = handlerType.GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

           foreach (var @interface in interfaces)
           {
               _handlerDescriptors.Add(new ServiceDescriptor(@interface, handlerType, handlerLifetime));
           }
       }

       // Scan for validators (Architecture Doc Section 6)
       var validators = assembly.GetTypes()
           .Where(t => t.IsClass && !t.IsAbstract)
           .Where(t => t.GetInterfaces().Any(i =>
               i.IsGenericType &&
               i.GetGenericTypeDefinition().Name == "IValidator`1")) // Forward reference
           .ToList();

       foreach (var validatorType in validators)
       {
           var interfaces = validatorType.GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IValidator`1");

           foreach (var @interface in interfaces)
           {
               // Validators always Scoped (can inject repositories)
               _validatorDescriptors.Add(new ServiceDescriptor(@interface, validatorType, ServiceLifetime.Scoped));
           }
       }

       // Scan for notification handlers
       var notificationHandlers = assembly.GetTypes()
           .Where(t => t.IsClass && !t.IsAbstract)
           .Where(t => t.GetInterfaces().Any(i =>
               i.IsGenericType &&
               i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
           .ToList();

       foreach (var handlerType in notificationHandlers)
       {
           var interfaces = handlerType.GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

           foreach (var @interface in interfaces)
           {
               _handlerDescriptors.Add(new ServiceDescriptor(@interface, handlerType, handlerLifetime));
           }
       }

       return this;
   }
   ```
2. Add interceptor registration methods:
   ```csharp
   /// <summary>
   /// Adds an interceptor type to the pipeline.
   /// </summary>
   public KommandConfiguration AddInterceptor(Type interceptorType)
   {
       if (interceptorType == null) throw new ArgumentNullException(nameof(interceptorType));
       if (!interceptorType.IsClass || interceptorType.IsAbstract)
           throw new ArgumentException("Interceptor must be a concrete class", nameof(interceptorType));

       _interceptorTypes.Add(interceptorType);
       return this;
   }

   /// <summary>
   /// Adds an interceptor type to the pipeline.
   /// </summary>
   public KommandConfiguration AddInterceptor<TInterceptor>() where TInterceptor : class
   {
       return AddInterceptor(typeof(TInterceptor));
   }
   ```
3. Add fluent helper methods (Architecture Doc Section 4, lines 454-467):
   ```csharp
   /// <summary>
   /// Enables validation by adding ValidationInterceptor.
   /// </summary>
   public KommandConfiguration WithValidation()
   {
       // Forward reference - ValidationInterceptor doesn't exist yet
       // This is a placeholder that will work once Phase 5 is complete
       return this;
   }
   ```
4. Add XML documentation to all methods

**How it fits**: Auto-discovers handlers, validators, and notification handlers from assemblies.

**Builds upon**: Task 1.12 (KommandConfiguration skeleton)

**Future dependencies**: Task 1.14 (ServiceCollectionExtensions calls these)

**Completion Checklist**:
- [ ] `RegisterHandlersFromAssembly()` method implemented
- [ ] Scans for ICommandHandler implementations
- [ ] Scans for IQueryHandler implementations
- [ ] Scans for INotificationHandler implementations
- [ ] Scans for IValidator implementations (placeholder)
- [ ] Respects lifetime parameter
- [ ] Validators always registered as Scoped
- [ ] `AddInterceptor(Type)` method implemented
- [ ] `AddInterceptor<T>()` generic method implemented
- [ ] `WithValidation()` placeholder method added
- [ ] Returns `this` for fluent chaining
- [ ] XML documentation complete
- [ ] Project builds successfully

---

### Task 1.14: Implement ServiceCollectionExtensions
**Status**: [x] Completed

**Objective**: Create extension method to register Kommand in DI (Architecture Doc Section 4, lines 298-342).

**Instructions**:
1. In `src/Kommand/Registration/`, create `ServiceCollectionExtensions.cs`:
   ```csharp
   namespace Microsoft.Extensions.DependencyInjection;

   using Kommand;

   /// <summary>
   /// Extension methods for registering Kommand in IServiceCollection.
   /// </summary>
   public static class ServiceCollectionExtensions
   {
       /// <summary>
       /// Registers Kommand mediator and handlers in the service collection.
       /// </summary>
       public static IServiceCollection AddKommand(
           this IServiceCollection services,
           Action<KommandConfiguration> configure)
       {
           if (services == null) throw new ArgumentNullException(nameof(services));
           if (configure == null) throw new ArgumentNullException(nameof(configure));

           var config = new KommandConfiguration();
           configure(config);

           // Register IMediator as Scoped (NOT Singleton!)
           services.AddScoped<IMediator, Mediator>();

           // Register all handlers
           foreach (var descriptor in config.HandlerDescriptors)
           {
               services.Add(descriptor);
           }

           // Register all validators
           foreach (var descriptor in config.ValidatorDescriptors)
           {
               services.Add(descriptor);
           }

           // Register OTEL interceptors (placeholder - will be implemented in Phase 3)
           // services.TryAddSingleton<ActivityInterceptor>();
           // services.TryAddSingleton<MetricsInterceptor>();

           // Register user interceptors
           foreach (var interceptorType in config.InterceptorTypes)
           {
               services.AddScoped(interceptorType);
           }

           return services;
       }
   }
   ```
2. Note: OTEL registration is commented out as placeholder for Phase 3
3. Add comprehensive XML documentation with usage examples
4. Verify namespace is `Microsoft.Extensions.DependencyInjection` for discoverability

**How it fits**: This is the entry point users call in Program.cs to configure Kommand.

**Builds upon**:
- Task 1.12-1.13 (KommandConfiguration)
- Task 1.9-1.11 (Mediator implementation)

**Future dependencies**:
- Task 1.15 (will be tested)
- Task 4.1 (OTEL registration)

**Completion Checklist**:
- [ ] `ServiceCollectionExtensions` class in `Registration/` folder
- [ ] Class is public static
- [ ] In `Microsoft.Extensions.DependencyInjection` namespace
- [ ] `AddKommand()` extension method implemented
- [ ] Registers IMediator as Scoped
- [ ] Loops through and registers all handlers
- [ ] Loops through and registers all validators
- [ ] Loops through and registers all interceptors
- [ ] Validates input parameters
- [ ] Returns IServiceCollection for chaining
- [ ] XML documentation with usage examples
- [ ] Project builds successfully

---

### Task 1.15: Create Test Project and Write Basic Tests
**Status**: [ ] Not Started

**Objective**: Create a single test project with both unit and integration tests (Architecture Doc Section 12).

**Instructions**:
1. Create test project:
   ```bash
   cd tests
   dotnet new xunit -n Kommand.Tests -f net8.0
   cd ..
   dotnet sln add tests/Kommand.Tests
   ```
   Note: Test project targets net8.0 (single target is fine for tests)
2. Add project references and packages:
   ```bash
   cd tests/Kommand.Tests
   dotnet add reference ../../src/Kommand/Kommand.csproj
   dotnet add package Microsoft.Extensions.DependencyInjection
   dotnet add package FluentAssertions
   dotnet add package NSubstitute
   ```
3. Create folder structure:
   ```bash
   mkdir Integration
   mkdir Unit
   ```
4. Create test file `Integration/BasicIntegrationTests.cs`:
   ```csharp
   namespace Kommand.Tests.Integration;

   using Microsoft.Extensions.DependencyInjection;
   using FluentAssertions;

   public class BasicIntegrationTests
   {
       [Fact]
       public async Task SendAsync_WithRegisteredHandler_ShouldInvokeHandlerAndReturnResult()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();

           // Act
           var result = await mediator.SendAsync(new TestCommand("test"), CancellationToken.None);

           // Assert
           result.Should().Be("test-handled");
       }

       [Fact]
       public async Task QueryAsync_WithRegisteredHandler_ShouldInvokeHandlerAndReturnResult()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestQuery).Assembly);
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();

           // Act
           var result = await mediator.QueryAsync(new TestQuery(42), CancellationToken.None);

           // Assert
           result.Should().Be(84);
       }

       [Fact]
       public async Task PublishAsync_WithMultipleHandlers_ShouldInvokeAllHandlers()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddSingleton<TestNotificationTracker>();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestNotification).Assembly);
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();
           var tracker = provider.GetRequiredService<TestNotificationTracker>();

           // Act
           await mediator.PublishAsync(new TestNotification(), CancellationToken.None);

           // Assert
           tracker.CallCount.Should().Be(2);
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

   public record TestQuery(int Value) : IQuery<int>;

   public class TestQueryHandler : IQueryHandler<TestQuery, int>
   {
       public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken)
       {
           return Task.FromResult(query.Value * 2);
       }
   }

   public record TestNotification : INotification;

   public class TestNotificationHandler1 : INotificationHandler<TestNotification>
   {
       private readonly TestNotificationTracker _tracker;
       public TestNotificationHandler1(TestNotificationTracker tracker) => _tracker = tracker;

       public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
       {
           _tracker.Increment();
           return Task.CompletedTask;
       }
   }

   public class TestNotificationHandler2 : INotificationHandler<TestNotification>
   {
       private readonly TestNotificationTracker _tracker;
       public TestNotificationHandler2(TestNotificationTracker tracker) => _tracker = tracker;

       public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken)
       {
           _tracker.Increment();
           return Task.CompletedTask;
       }
   }

   public class TestNotificationTracker
   {
       public int CallCount { get; private set; }
       public void Increment() => CallCount++;
   }
   ```
5. Run tests: `dotnet test`

**How it fits**: Validates that Phase 1 core functionality works end-to-end.

**Builds upon**: All Phase 1 tasks (1.1-1.14)

**Future dependencies**: Pattern for integration tests in later phases.

**Completion Checklist**:
- [ ] `Kommand.Tests` project created and added to solution
- [ ] Project references Kommand and required packages (including NSubstitute)
- [ ] `Integration/` and `Unit/` folders created
- [ ] All three integration test methods implemented
- [ ] Test command, query, and handlers created
- [ ] Test notification and handlers created
- [ ] Tests use full DI container (realistic scenario)
- [ ] All tests pass: `dotnet test`
- [ ] Tests use FluentAssertions
- [ ] Tests follow AAA pattern

---

## Phase 2: Interceptor System (8 tasks)

### Task 2.1: Implement Interceptor Abstractions
**Status**: [ ] Not Started

**Objective**: Create interceptor interfaces for cross-cutting concerns (Architecture Doc Section 3, lines 175-210).

**Instructions**:
1. In `src/Kommand/Interceptors/`, create `RequestHandlerDelegate.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Represents the next handler in the interceptor pipeline.
   /// </summary>
   /// <typeparam name="TResponse">The response type</typeparam>
   public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
   ```
2. Create `IInterceptor.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Interceptor for all requests (commands and queries).
   /// </summary>
   public interface IInterceptor<in TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       /// <summary>
       /// Handles the request and calls the next handler in the pipeline.
       /// </summary>
       Task<TResponse> HandleAsync(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken);
   }
   ```
3. Create `ICommandInterceptor.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Interceptor specifically for commands.
   /// </summary>
   public interface ICommandInterceptor<in TCommand, TResponse>
       where TCommand : ICommand<TResponse>
   {
       /// <summary>
       /// Handles the command and calls the next handler in the pipeline.
       /// </summary>
       Task<TResponse> HandleAsync(
           TCommand command,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken);
   }
   ```
4. Create `IQueryInterceptor.cs`:
   ```csharp
   namespace Kommand;

   /// <summary>
   /// Interceptor specifically for queries.
   /// </summary>
   public interface IQueryInterceptor<in TQuery, TResponse>
       where TQuery : IQuery<TResponse>
   {
       /// <summary>
       /// Handles the query and calls the next handler in the pipeline.
       /// </summary>
       Task<TResponse> HandleAsync(
           TQuery query,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken);
   }
   ```
5. Add comprehensive XML documentation explaining:
   - What interceptors are (cross-cutting concerns)
   - Execution order (outermost to innermost)
   - How to call `next()` to continue the pipeline
   - Short-circuiting (not calling `next()`)
   - Common use cases (logging, validation, caching, etc.)
   - Example implementations

**How it fits**: Foundation for all interceptor functionality (logging, validation, OTEL, etc.).

**Builds upon**: Task 1.4 (ICommand/IQuery/IRequest)

**Future dependencies**:
- Task 2.2 (integrate into Mediator)
- Task 4.1 (OTEL interceptors)
- Task 5.2 (ValidationInterceptor)

**Completion Checklist**:
- [ ] `RequestHandlerDelegate<TResponse>` delegate created
- [ ] `IInterceptor<TRequest, TResponse>` interface created
- [ ] `ICommandInterceptor<TCommand, TResponse>` interface created
- [ ] `IQueryInterceptor<TQuery, TResponse>` interface created
- [ ] All have `HandleAsync` method with `next` parameter
- [ ] Generic constraints properly applied
- [ ] Comprehensive XML documentation with examples
- [ ] Explains short-circuit behavior
- [ ] Project builds successfully

---

### Task 2.2: Integrate Interceptors into Mediator - Part 1 (Pipeline Building)
**Status**: [ ] Not Started

**Objective**: Modify Mediator to build and execute interceptor pipeline (Architecture Doc Section 3).

**Instructions**:
1. In `src/Kommand/Implementation/Mediator.cs`, add method to build interceptor pipeline:
   ```csharp
   private RequestHandlerDelegate<TResponse> BuildPipeline<TRequest, TResponse>(
       TRequest request,
       Func<Task<TResponse>> handlerFunc,
       CancellationToken cancellationToken)
       where TRequest : IRequest<TResponse>
   {
       RequestHandlerDelegate<TResponse> pipeline = () => handlerFunc();

       // Resolve interceptors for this request type
       var interceptorType = typeof(IInterceptor<,>).MakeGenericType(typeof(TRequest), typeof(TResponse));
       var interceptors = _serviceProvider.GetService(
           typeof(IEnumerable<>).MakeGenericType(interceptorType)) as IEnumerable<object>;

       if (interceptors != null && interceptors.Any())
       {
           // Build pipeline in reverse order (last interceptor wraps handler)
           foreach (var interceptor in interceptors.Reverse())
           {
               var current = pipeline;
               var handleMethod = interceptorType.GetMethod(nameof(IInterceptor<IRequest<TResponse>, TResponse>.HandleAsync));

               pipeline = () => (Task<TResponse>)handleMethod!.Invoke(
                   interceptor,
                   new object[] { request, current, cancellationToken })!;
           }
       }

       return pipeline;
   }
   ```
2. Do NOT modify SendAsync/QueryAsync yet (that's next task)
3. Add XML documentation explaining pipeline construction

**How it fits**: Creates the interceptor chain that wraps handler execution.

**Builds upon**:
- Task 1.9-1.11 (Mediator implementation)
- Task 2.1 (interceptor interfaces)

**Future dependencies**: Task 2.3 (use this in SendAsync/QueryAsync)

**Completion Checklist**:
- [ ] `BuildPipeline<TRequest, TResponse>()` method added to Mediator
- [ ] Method is private
- [ ] Resolves all interceptors for request type
- [ ] Builds pipeline in reverse order
- [ ] Returns RequestHandlerDelegate
- [ ] Uses reflection to invoke interceptors
- [ ] XML documentation explains pipeline construction
- [ ] Project builds successfully

---

### Task 2.3: Integrate Interceptors into Mediator - Part 2 (Use Pipeline)
**Status**: [ ] Not Started

**Objective**: Modify SendAsync and QueryAsync to use interceptor pipeline.

**Instructions**:
1. In `src/Kommand/Implementation/Mediator.cs`, modify `SendAsync<TResponse>`:
   ```csharp
   public async Task<TResponse> SendAsync<TResponse>(
       ICommand<TResponse> command,
       CancellationToken cancellationToken = default)
   {
       if (command == null) throw new ArgumentNullException(nameof(command));

       var commandType = command.GetType();
       var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
       var handler = _serviceProvider.GetService(handlerType);

       if (handler == null)
       {
           throw new InvalidOperationException($"No handler registered for command {commandType.Name}");
       }

       var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.HandleAsync));

       // Build handler function
       Func<Task<TResponse>> handlerFunc = () =>
           (Task<TResponse>)handleMethod!.Invoke(handler, new object[] { command, cancellationToken })!;

       // Build and execute pipeline
       var pipeline = BuildPipeline<ICommand<TResponse>, TResponse>(command, handlerFunc, cancellationToken);
       return await pipeline();
   }
   ```
2. Apply same pattern to `SendAsync(ICommand)` and `QueryAsync<TResponse>`
3. Verify behavior: if no interceptors, should directly invoke handler
4. Update XML documentation

**How it fits**: Commands and queries now flow through interceptor pipeline before reaching handlers.

**Builds upon**: Task 2.2 (BuildPipeline method)

**Future dependencies**:
- Task 4.1 (OTEL interceptors will execute)
- Task 5.2 (ValidationInterceptor will execute)

**Completion Checklist**:
- [ ] `SendAsync<TResponse>` modified to use BuildPipeline
- [ ] `SendAsync(ICommand)` modified to use BuildPipeline
- [ ] `QueryAsync<TResponse>` modified to use BuildPipeline
- [ ] Handler resolution logic unchanged
- [ ] Pipeline built before handler execution
- [ ] XML documentation updated
- [ ] Integration test from Task 1.15 still passes
- [ ] Project builds successfully

---

### Task 2.4: Create Example Logging Interceptor (Documentation Only)
**Status**: [ ] Not Started

**Objective**: Create reference implementation for documentation (Architecture Doc Section 3, lines 226-245).

**Instructions**:
1. Create `samples/` directory at repository root if not exists
2. Create `samples/Kommand.Sample/` project:
   ```bash
   cd samples
   dotnet new console -n Kommand.Sample
   cd ..
   dotnet sln add samples/Kommand.Sample
   cd samples/Kommand.Sample
   dotnet add reference ../../src/Kommand/Kommand.csproj
   dotnet add package Microsoft.Extensions.Logging.Abstractions
   ```
3. Create `Interceptors/LoggingInterceptor.cs`:
   ```csharp
   namespace Kommand.Sample.Interceptors;

   using Microsoft.Extensions.Logging;

   /// <summary>
   /// Example interceptor that logs request execution.
   /// This is a sample implementation for documentation purposes.
   /// </summary>
   public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

       public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>> logger)
       {
           _logger = logger;
       }

       public async Task<TResponse> HandleAsync(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken)
       {
           var requestName = typeof(TRequest).Name;
           var startTime = DateTime.UtcNow;

           _logger.LogInformation("Executing request {RequestName}", requestName);

           try
           {
               var response = await next(); // Call next handler in pipeline

               var duration = DateTime.UtcNow - startTime;
               _logger.LogInformation(
                   "Request {RequestName} completed successfully in {Duration}ms",
                   requestName,
                   duration.TotalMilliseconds);

               return response;
           }
           catch (Exception ex)
           {
               var duration = DateTime.UtcNow - startTime;
               _logger.LogError(
                   ex,
                   "Request {RequestName} failed after {Duration}ms",
                   requestName,
                   duration.TotalMilliseconds);
               throw;
           }
       }
   }
   ```
4. Add comprehensive comments explaining:
   - How to inject dependencies (ILogger)
   - How to call `next()`
   - How to wrap with try-catch
   - How to measure duration
   - How to log before/after execution
5. This is example code for users to copy/reference

**How it fits**: Provides users with working example of custom interceptor.

**Builds upon**: Task 2.1 (IInterceptor interface)

**Future dependencies**: Referenced in documentation tasks.

**Completion Checklist**:
- [ ] Sample project created and added to solution
- [ ] LoggingInterceptor created in samples
- [ ] Uses ILogger correctly
- [ ] Measures execution duration
- [ ] Logs before and after execution
- [ ] Handles exceptions and re-throws
- [ ] Comprehensive inline comments
- [ ] Code is clean and readable
- [ ] Can be used as reference by users

---

### Task 2.5: Write Unit Tests for Interceptor Pipeline
**Status**: [ ] Not Started

**Objective**: Test interceptor execution and ordering (Architecture Doc Section 12).

**Instructions**:
1. Create test file `Unit/InterceptorTests.cs` in the existing `Kommand.Tests` project:
   ```csharp
   namespace Kommand.Tests.Unit;

   using Microsoft.Extensions.DependencyInjection;
   using FluentAssertions;

   public class InterceptorTests
   {
       [Fact]
       public async Task SendAsync_WithNoInterceptors_ShouldInvokeHandlerDirectly()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();

           // Act
           var result = await mediator.SendAsync(new TestCommand("test"));

           // Assert
           result.Should().Be("test-handled");
       }

       [Fact]
       public async Task SendAsync_WithOneInterceptor_ShouldExecuteInterceptor()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddSingleton<ExecutionTracker>();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
               config.AddInterceptor<TrackingInterceptor>();
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();
           var tracker = provider.GetRequiredService<ExecutionTracker>();

           // Act
           await mediator.SendAsync(new TestCommand("test"));

           // Assert
           tracker.ExecutionLog.Should().ContainInOrder(
               "Interceptor-Enter",
               "Handler",
               "Interceptor-Exit"
           );
       }

       [Fact]
       public async Task SendAsync_WithMultipleInterceptors_ShouldExecuteInCorrectOrder()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddSingleton<ExecutionTracker>();
           services.AddKommand(config =>
           {
               config.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
               config.AddInterceptor<Interceptor1>();
               config.AddInterceptor<Interceptor2>();
           });
           var provider = services.BuildServiceProvider();
           var mediator = provider.GetRequiredService<IMediator>();
           var tracker = provider.GetRequiredService<ExecutionTracker>();

           // Act
           await mediator.SendAsync(new TestCommand("test"));

           // Assert
           tracker.ExecutionLog.Should().ContainInOrder(
               "Interceptor1-Enter",
               "Interceptor2-Enter",
               "Handler",
               "Interceptor2-Exit",
               "Interceptor1-Exit"
           );
       }
   }

   // Test fixtures
   public record TestCommand(string Value) : ICommand<string>;

   public class TestCommandHandler : ICommandHandler<TestCommand, string>
   {
       private readonly ExecutionTracker _tracker;
       public TestCommandHandler(ExecutionTracker tracker) => _tracker = tracker;

       public Task<string> HandleAsync(TestCommand command, CancellationToken ct)
       {
           _tracker.Log("Handler");
           return Task.FromResult(command.Value + "-handled");
       }
   }

   public class TrackingInterceptor : IInterceptor<TestCommand, string>
   {
       private readonly ExecutionTracker _tracker;
       public TrackingInterceptor(ExecutionTracker tracker) => _tracker = tracker;

       public async Task<string> HandleAsync(
           TestCommand request,
           RequestHandlerDelegate<string> next,
           CancellationToken ct)
       {
           _tracker.Log("Interceptor-Enter");
           var result = await next();
           _tracker.Log("Interceptor-Exit");
           return result;
       }
   }

   public class Interceptor1 : IInterceptor<TestCommand, string>
   {
       private readonly ExecutionTracker _tracker;
       public Interceptor1(ExecutionTracker tracker) => _tracker = tracker;

       public async Task<string> HandleAsync(
           TestCommand request,
           RequestHandlerDelegate<string> next,
           CancellationToken ct)
       {
           _tracker.Log("Interceptor1-Enter");
           var result = await next();
           _tracker.Log("Interceptor1-Exit");
           return result;
       }
   }

   public class Interceptor2 : IInterceptor<TestCommand, string>
   {
       private readonly ExecutionTracker _tracker;
       public Interceptor2(ExecutionTracker tracker) => _tracker = tracker;

       public async Task<string> HandleAsync(
           TestCommand request,
           RequestHandlerDelegate<string> next,
           CancellationToken ct)
       {
           _tracker.Log("Interceptor2-Enter");
           var result = await next();
           _tracker.Log("Interceptor2-Exit");
           return result;
       }
   }

   public class ExecutionTracker
   {
       private readonly List<string> _log = new();
       public IReadOnlyList<string> ExecutionLog => _log;
       public void Log(string message) => _log.Add(message);
   }
   ```
2. Run tests: `dotnet test`

**How it fits**: Ensures interceptor pipeline works correctly.

**Builds upon**:
- Task 2.3 (interceptor integration)
- Task 1.15 (test project already created)

**Future dependencies**: Pattern for testing future interceptors.

**Completion Checklist**:
- [ ] `Unit/InterceptorTests.cs` file created
- [ ] All three test scenarios implemented
- [ ] Test helpers (ExecutionTracker) created
- [ ] Execution order verified
- [ ] Tests use full DI container
- [ ] All tests pass
- [ ] Tests use FluentAssertions
- [ ] All tests in same namespace: `Kommand.Tests.Unit`

---

[Continued in next part due to length...]

## Phase 3: OpenTelemetry Integration (5 tasks)
## Phase 4: Validation System (6 tasks)
## Phase 5: Notifications & Polish (5 tasks)
## Phase 6: Documentation & Release (8 tasks)

---

## Summary

**Total Tasks**: 52 tasks
**Estimated Total Time**: ~52 hours (1 hour per task average)
**Completion Rate**: 15.4% (8/52 complete)

### Phase Breakdown
- **Phase 1: Core Foundation** - 15 tasks (~15 hours)
- **Phase 2: Interceptor System** - 8 tasks (~8 hours)
- **Phase 3: OpenTelemetry Integration** - 5 tasks (~5 hours)
- **Phase 4: Validation System** - 6 tasks (~6 hours)
- **Phase 5: Notifications & Polish** - 5 tasks (~5 hours)
- **Phase 6: Documentation & Release** - 8 tasks (~8 hours)

### Notes
1. **Test Project**: This task list uses a single test project (`Kommand.Tests`) with `Unit/` and `Integration/` folders, rather than separate projects. This simplifies the initial implementation while maintaining clear organization.
2. **Accuracy**: This task list is **accurate to the architecture document**. Every task references specific sections and line numbers from `MEDIATOR_ARCHITECTURE_PLAN.md`.
3. **Phases 3-6**: The remaining phases (OpenTelemetry, Validation, Notifications, Documentation) will be detailed in the next iteration to keep this file manageable.