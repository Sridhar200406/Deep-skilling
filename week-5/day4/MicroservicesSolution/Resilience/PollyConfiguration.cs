using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Resilience
{
    /// <summary>
    /// Polly resilience policy factory.
    ///
    /// Policies:
    ///   Retry          — 3 retries with exponential backoff (2s, 4s, 8s) on transient HTTP errors
    ///   Circuit Breaker— opens after 5 failures in 30s; stays open for 15s
    ///   Timeout        — per-request timeout of 10 seconds
    ///   Fallback       — returns null/default when all else fails
    ///
    /// Usage in Program.cs:
    ///   builder.Services.AddHttpClient&lt;DepartmentServiceClient&gt;(...)
    ///       .AddPolicyHandler(PollyConfiguration.GetRetryPolicy(logger))
    ///       .AddPolicyHandler(PollyConfiguration.GetCircuitBreakerPolicy(logger))
    ///       .AddPolicyHandler(PollyConfiguration.GetTimeoutPolicy());
    /// </summary>
    public static class PollyConfiguration
    {
        // ── Retry Policy ──────────────────────────────────────────────────────
        /// <summary>
        /// Retries up to 3 times on transient HTTP errors (5xx, 408, network failures).
        /// Uses exponential back-off: 2^attempt seconds.
        /// Logs each retry attempt.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger) =>
            HttpPolicyExtensions
                .HandleTransientHttpError()          // 5xx + 408 + network exceptions
                .OrResult(r => (int)r.StatusCode == 429)  // also retry on 429 Too Many Requests
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, attempt, context) =>
                    {
                        logger.LogWarning(
                            "Polly Retry: attempt {Attempt} after {Delay}s. " +
                            "Reason: {Reason}. CorrelationId: {CorrelationId}",
                            attempt,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString(),
                            context.CorrelationId);
                    });

        // ── Circuit Breaker Policy ────────────────────────────────────────────
        /// <summary>
        /// Opens the circuit after 5 consecutive failures in a 30-second window.
        /// Stays open for 15 seconds before allowing a probe request.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger) =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold:         0.5,   // 50% failure rate
                    samplingDuration:         TimeSpan.FromSeconds(30),
                    minimumThroughput:        5,      // at least 5 requests before evaluating
                    durationOfBreak:          TimeSpan.FromSeconds(15),
                    onBreak: (outcome, breakDuration, context) =>
                    {
                        logger.LogError(
                            "Polly CircuitBreaker: OPEN for {BreakDuration}s. " +
                            "Reason: {Reason}. CorrelationId: {CorrelationId}",
                            breakDuration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
                            context.CorrelationId);
                    },
                    onReset: context =>
                    {
                        logger.LogInformation(
                            "Polly CircuitBreaker: CLOSED (reset). CorrelationId: {CorrelationId}",
                            context.CorrelationId);
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Polly CircuitBreaker: HALF-OPEN — probe request allowed.");
                    });

        // ── Timeout Policy ────────────────────────────────────────────────────
        /// <summary>
        /// Per-request timeout of 10 seconds.
        /// Times out before Retry/Circuit-Breaker so each attempt gets its own timeout.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int seconds = 10) =>
            Policy.TimeoutAsync<HttpResponseMessage>(
                seconds,
                TimeoutStrategy.Optimistic);

        // ── Fallback Policy ───────────────────────────────────────────────────
        /// <summary>
        /// Returns a 503 Service Unavailable fallback response when the circuit is open
        /// or all retries are exhausted, instead of throwing an exception.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(ILogger logger) =>
            Policy<HttpResponseMessage>
                .Handle<BrokenCircuitException>()
                .Or<TimeoutRejectedException>()
                .Or<HttpRequestException>()
                .FallbackAsync(
                    fallbackValue: new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(
                            """{"success":false,"message":"Downstream service is unavailable."}""")
                    },
                    onFallbackAsync: (outcome, context) =>
                    {
                        logger.LogError(
                            "Polly Fallback: returning 503 for CorrelationId={CorrelationId}. Reason: {Reason}",
                            context.CorrelationId,
                            outcome.Exception?.Message ?? "Unknown");
                        return Task.CompletedTask;
                    });

        // ── Combined Policy wrap helper ───────────────────────────────────────
        /// <summary>
        /// Returns a combined policy: Fallback → CircuitBreaker → Retry → Timeout (inner-most first).
        /// Attach to AddHttpClient(...).AddPolicyHandler(PollyConfiguration.GetCombinedPolicy(logger))
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger) =>
            Policy.WrapAsync(
                GetFallbackPolicy(logger),
                GetCircuitBreakerPolicy(logger),
                GetRetryPolicy(logger),
                GetTimeoutPolicy());
    }
}
