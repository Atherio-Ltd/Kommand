using System.Text.Json;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Interceptors;

/// <summary>
/// Command-only interceptor that creates an audit trail for all write operations.
/// Demonstrates ICommandInterceptor - only intercepts commands, not queries.
/// </summary>
/// <remarks>
/// Use cases for command-only interceptors:
/// - Audit logging (who changed what, when)
/// - Transaction management (wrap commands in DB transactions)
/// - Authorization checks for write operations
/// - Domain event publishing after successful commands
/// - Idempotency checks to prevent duplicate operations
/// </remarks>
public class CommandAuditInterceptor<TCommand, TResponse> : ICommandInterceptor<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly ILogger<CommandAuditInterceptor<TCommand, TResponse>> _logger;

    public CommandAuditInterceptor(ILogger<CommandAuditInterceptor<TCommand, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var commandId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow;

        // Serialize command payload for audit (in production, be careful with sensitive data)
        var payload = SerializeForAudit(command);

        _logger.LogInformation(
            "[AUDIT] Command started | ID: {CommandId} | Type: {CommandName} | Time: {Timestamp} | Payload: {Payload}",
            commandId,
            commandName,
            timestamp.ToString("O"),
            payload);

        try
        {
            var response = await next();

            var duration = DateTime.UtcNow - timestamp;

            _logger.LogInformation(
                "[AUDIT] Command succeeded | ID: {CommandId} | Type: {CommandName} | Duration: {Duration}ms",
                commandId,
                commandName,
                duration.TotalMilliseconds);

            // In production, you would persist this to an audit log table:
            // await _auditRepository.SaveAsync(new AuditEntry
            // {
            //     CommandId = commandId,
            //     CommandType = commandName,
            //     Payload = payload,
            //     UserId = _currentUser.Id,
            //     Timestamp = timestamp,
            //     Success = true,
            //     Duration = duration
            // });

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - timestamp;

            _logger.LogWarning(
                "[AUDIT] Command failed | ID: {CommandId} | Type: {CommandName} | Duration: {Duration}ms | Error: {Error}",
                commandId,
                commandName,
                duration.TotalMilliseconds,
                ex.Message);

            throw;
        }
    }

    private static string SerializeForAudit(TCommand command)
    {
        try
        {
            return JsonSerializer.Serialize(command, new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 3 // Prevent deep serialization of complex objects
            });
        }
        catch
        {
            return $"[{typeof(TCommand).Name}]"; // Fallback if serialization fails
        }
    }
}
