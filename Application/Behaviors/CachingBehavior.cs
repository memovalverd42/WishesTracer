// CachingBehavior.cs - MediatR pipeline behavior for distributed caching
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WishesTracer.Application.Interfaces;

namespace WishesTracer.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that implements distributed caching for queries.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
/// <remarks>
/// This behavior intercepts queries implementing ICacheableQuery and caches their results
/// in Redis. On subsequent requests with the same cache key, the cached result is returned
/// without executing the handler, significantly improving performance for read operations.
/// </remarks>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the CachingBehavior class.
    /// </summary>
    /// <param name="cache">The distributed cache implementation (Redis)</param>
    /// <param name="logger">Logger for cache hit/miss diagnostics</param>
    public CachingBehavior(IDistributedCache cache, 
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request by checking cache first, then executing the handler if needed.
    /// </summary>
    /// <param name="request">The incoming request</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached or freshly computed response</returns>
    public async Task<TResponse> Handle(TRequest request, 
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Verificar si la request implementa ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cacheKey = cacheableQuery.CacheKey;

        // 2. Intentar obtener de Redis
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("🚀 Cache HIT para {Key}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedData)!;
        }

        _logger.LogInformation("🐌 Cache MISS para {Key}. Ejecutando Handler...", cacheKey);

        // 3. Ejecutar el Handler real (Ir a la BD)
        var response = await next(cancellationToken);

        // 4. Guardar en Redis (Solo si la respuesta es exitosa/válida)
        if (response != null) 
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableQuery.Expiration 
                    ?? TimeSpan.FromMinutes(10)
            };

            var serializedData = JsonSerializer.Serialize(response);
            await _cache.SetStringAsync(cacheKey, serializedData, options, cancellationToken);
        }

        return response;
    }
}
