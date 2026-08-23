using Microsoft.Extensions.DependencyInjection;

namespace AtlasBank.Shared.Resilience;

public static class GrpcResilienceExtensions
{
    /// <summary>
    /// Applies a Polly resilience pipeline (timeout, retry, circuit breaker) tuned for
    /// internal service-to-service gRPC calls on the same Docker network, rather than
    /// .NET's stock <c>AddStandardResilienceHandler()</c> defaults — those are sized for
    /// calls to third-party APIs across the internet (10s per attempt, 30s total,
    /// a circuit breaker that needs 100 requests before it can trip) and would be far too
    /// slow to notice a genuinely stuck downstream call, or too insensitive to ever open
    /// for a lower-traffic internal service.
    /// </summary>
    /// <param name="allowRetry">
    /// Set to false for a client whose RPCs include a non-idempotent write (e.g. a
    /// Credit/Debit balance change). Retrying a call whose response was lost after the
    /// write already landed would silently apply it twice — timeout and circuit breaker
    /// are always safe regardless, but automatic retry is only safe for calls that are
    /// reads or are otherwise idempotent.
    /// </param>
    public static IHttpClientBuilder AddGrpcResilienceHandler(this IHttpClientBuilder builder, bool allowRetry = true)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);
            options.Retry.MaxRetryAttempts = allowRetry ? 3 : 0;
            // A lower-traffic internal service would rarely reach the standard handler's
            // default 100-request sampling minimum, so its circuit breaker could never
            // trip during a real outage. 10 is enough to detect a genuinely broken
            // dependency without reacting to a couple of one-off blips.
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        });
        return builder;
    }
}
