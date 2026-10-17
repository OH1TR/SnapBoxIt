using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

// Oh Microsoft, why can't we have long lasting auth tokens on Azure?

namespace SnapBoxApi.Middleware
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BasicAuthMiddleware> _logger;

        // Clients store the password, so a legitimate user practically never fails.
        // A few failures from one address are treated as guessing.
        private readonly int _maxFailures;
        private readonly TimeSpan _lockout;

        public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration, IMemoryCache cache, ILogger<BasicAuthMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            _maxFailures = configuration.GetValue("BasicAuth:MaxFailures", 3);
            _lockout = TimeSpan.FromMinutes(configuration.GetValue("BasicAuth:LockoutMinutes", 360));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health checks or other non-API endpoints
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            var clientKey = $"auth-failures:{context.Connection.RemoteIpAddress}";

            // A locked address is rejected before the credentials are even looked at,
            // so guessing cannot continue during the lockout.
            if (_cache.TryGetValue(clientKey, out FailedAttempts? attempts) && attempts!.LockedUntil is DateTimeOffset lockedUntil)
            {
                await TooManyAttemptsResponse(context, lockedUntil);
                return;
            }

            // Check if Authorization header exists
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                await UnauthorizedResponse(context);
                return;
            }

            if (!CredentialsAreValid(context.Request.Headers["Authorization"].ToString()))
            {
                RegisterFailure(context, clientKey);
                await UnauthorizedResponse(context);
                return;
            }

            _cache.Remove(clientKey);

            // Authentication successful, continue to next middleware
            await _next(context);
        }

        private bool CredentialsAreValid(string authHeader)
        {
            if (!authHeader.StartsWith("Basic "))
            {
                return false;
            }

            // Extract and decode credentials
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            string credentials;

            try
            {
                credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            }
            catch
            {
                return false;
            }

            var parts = credentials.Split(':', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            // Validate credentials against appsettings
            var configUsername = _configuration["BasicAuth:Username"];
            var configPassword = _configuration["BasicAuth:Password"];

            // Missing configuration must not let empty credentials through
            if (string.IsNullOrEmpty(configUsername) || string.IsNullOrEmpty(configPassword))
            {
                return false;
            }

            // Both are always compared (& instead of &&) so timing does not reveal which one was wrong
            return FixedTimeEquals(parts[0], configUsername) & FixedTimeEquals(parts[1], configPassword);
        }

        // Comparing hashes keeps the comparison constant-time regardless of the input lengths
        private static bool FixedTimeEquals(string a, string b)
        {
            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(a)),
                SHA256.HashData(Encoding.UTF8.GetBytes(b)));
        }

        private void RegisterFailure(HttpContext context, string clientKey)
        {
            var attempts = _cache.GetOrCreate(clientKey, entry =>
            {
                // The failure count is forgotten if no new failure arrives within the lockout period
                entry.SlidingExpiration = _lockout;
                return new FailedAttempts();
            })!;

            lock (attempts)
            {
                attempts.Count++;
                if (attempts.Count < _maxFailures || attempts.LockedUntil != null)
                {
                    return;
                }

                attempts.LockedUntil = DateTimeOffset.UtcNow.Add(_lockout);
            }

            // Re-insert with an absolute expiry so requests during the lockout do not extend it
            _cache.Set(clientKey, attempts, attempts.LockedUntil.Value);
            _logger.LogWarning("Locked {Address} for {Minutes} min after {Count} failed login attempts",
                context.Connection.RemoteIpAddress, _lockout.TotalMinutes, attempts.Count);
        }

        private async Task UnauthorizedResponse(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"SnapBoxIt API\"");
            await context.Response.WriteAsync("Unauthorized");
        }

        private static async Task TooManyAttemptsResponse(HttpContext context, DateTimeOffset lockedUntil)
        {
            var retryAfter = Math.Max(1, (int)Math.Ceiling((lockedUntil - DateTimeOffset.UtcNow).TotalSeconds));
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            await context.Response.WriteAsync("Too many failed login attempts");
        }

        private class FailedAttempts
        {
            public int Count;
            public DateTimeOffset? LockedUntil;
        }
    }
}
