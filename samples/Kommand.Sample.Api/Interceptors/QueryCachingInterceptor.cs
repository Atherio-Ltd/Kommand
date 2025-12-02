using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kommand;
using Kommand.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kommand.Sample.Api.Interceptors;

/// <summary>
/// Query-only interceptor that caches query results to improve performance.
/// Demonstrates IQueryInterceptor - only intercepts queries, not commands.
/// </summary>
/// <remarks>
/// Use cases for query-only interceptors:
/// - Result caching (queries are safe to cache since they don't modify state)
/// - Performance monitoring for slow queries
/// - Read replica routing (route queries to read-only database replicas)
/// - Response transformation/projection
/// - Rate limiting for expensive queries
///
/// This sample uses an in-memory cache for demonstration.
/// In production, use IDistributedCache or a proper caching solution.
/// </remarks>
public class QueryCachingInterceptor<TQuery, TResponse> : IQueryInterceptor<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly ILogger<QueryCachingInterceptor<TQuery, TResponse>> _logger;

    // Simple in-memory cache for demonstration
    // In production, inject IDistributedCache or IMemoryCache
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromSeconds(30);

    public QueryCachingInterceptor(ILogger<QueryCachingInterceptor<TQuery, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TQuery).Name;
        var cacheKey = GenerateCacheKey(query);

        // Try to get from cache
        if (TryGetFromCache(cacheKey, out var cachedResponse))
        {
            _logger.LogInformation(
                "[CACHE] HIT | Query: {QueryName} | Key: {CacheKey}",
                queryName,
                cacheKey[..16] + "...");

            return cachedResponse!;
        }

        _logger.LogInformation(
            "[CACHE] MISS | Query: {QueryName} | Key: {CacheKey}",
            queryName,
            cacheKey[..16] + "...");

        // Execute query handler
        var response = await next();

        // Cache the result
        AddToCache(cacheKey, response);

        _logger.LogInformation(
            "[CACHE] STORED | Query: {QueryName} | TTL: {TTL}s",
            queryName,
            DefaultCacheDuration.TotalSeconds);

        return response;
    }

    private static string GenerateCacheKey(TQuery query)
    {
        var queryType = typeof(TQuery).FullName ?? typeof(TQuery).Name;
        var queryJson = JsonSerializer.Serialize(query);
        var combined = $"{queryType}:{queryJson}";

        // Create a hash for a shorter, consistent key
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes);
    }

    private bool TryGetFromCache(string key, out TResponse? response)
    {
        response = default;

        if (!Cache.TryGetValue(key, out var entry))
            return false;

        // Check if expired
        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            Cache.TryRemove(key, out _);
            return false;
        }

        // Deserialize cached value
        try
        {
            response = JsonSerializer.Deserialize<TResponse>(entry.SerializedValue);
            return response != null;
        }
        catch
        {
            Cache.TryRemove(key, out _);
            return false;
        }
    }

    private static void AddToCache(string key, TResponse response)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(response);
            var entry = new CacheEntry(serialized, DateTime.UtcNow.Add(DefaultCacheDuration));

            Cache.AddOrUpdate(key, entry, (_, _) => entry);

            // Simple cache cleanup - remove expired entries periodically
            if (Cache.Count > 100)
            {
                CleanupExpiredEntries();
            }
        }
        catch
        {
            // Ignore serialization errors - just don't cache
        }
    }

    private static void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = Cache
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            Cache.TryRemove(key, out _);
        }
    }

    private record CacheEntry(string SerializedValue, DateTime ExpiresAt);
}
