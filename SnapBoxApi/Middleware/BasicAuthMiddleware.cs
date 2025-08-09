using System.Text;

// Oh Microsoft, why can't we have long lasting auth tokens on Azure?

namespace SnapBoxApi.Middleware
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health checks or other non-API endpoints
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // Check if Authorization header exists
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                await UnauthorizedResponse(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            
            if (!authHeader.StartsWith("Basic "))
            {
                await UnauthorizedResponse(context);
                return;
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
                await UnauthorizedResponse(context);
                return;
            }

            var parts = credentials.Split(':', 2);
            if (parts.Length != 2)
            {
                await UnauthorizedResponse(context);
                return;
            }

            var username = parts[0];
            var password = parts[1];

            // Validate credentials against appsettings
            var configUsername = _configuration["BasicAuth:Username"];
            var configPassword = _configuration["BasicAuth:Password"];

            if (username != configUsername || password != configPassword)
            {
                await UnauthorizedResponse(context);
                return;
            }

            // Authentication successful, continue to next middleware
            await _next(context);
        }

        private async Task UnauthorizedResponse(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"SnapBoxIt API\"");
            await context.Response.WriteAsync("Unauthorized");
        }
    }
}
