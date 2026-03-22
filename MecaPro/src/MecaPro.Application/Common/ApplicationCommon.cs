using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MecaPro.Application.Common;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string[]? Errors { get; }

    protected Result(bool success, T? value, string? error, string[]? errors = null)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(string[] errors) => new(false, default, null, errors);
}

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("[PROJET DASH] Handling {Name}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("[PROJET DASH] Handled {Name}", typeof(TRequest).Name);
        return response;
    }
}

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}

public class CachingBehavior<TRequest, TResponse>(IDistributedCache cache, ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableRequest cacheable) return await next();
        var cached = await cache.GetStringAsync(cacheable.CacheKey, ct);
        if (cached != null)
        {
            logger.LogDebug("[CACHE HIT] {Key}", cacheable.CacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }
        var response = await next();
        var json = JsonSerializer.Serialize(response);
        await cache.SetStringAsync(cacheable.CacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration }, ct);
        return response;
    }
}

public class TransactionBehavior<TRequest, TResponse>(MecaPro.Domain.Common.IUnitOfWork uow) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!typeof(TRequest).Name.EndsWith("Command")) return await next();
        var response = await next();
        await uow.SaveChangesAsync(ct);
        return response;
    }
}
