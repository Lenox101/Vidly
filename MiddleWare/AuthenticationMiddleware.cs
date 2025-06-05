namespace Vidly.MiddleWare
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Lowercased paths for case-insensitive comparison
            var publicPaths = new[] { "/auth/login", "/auth/signup", "/" };
            var path = context.Request.Path.Value?.ToLowerInvariant();

            _logger.LogInformation("Processing request for path: {Path}", path);

            // Allow static file requests
            if (path != null &&
                (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib") || path.EndsWith(".png")))
            {
                await _next(context);
                return;
            }

            // Allow access to public paths
            if (path != null && publicPaths.Contains(path))
            {
                _logger.LogInformation("Accessing public path: {Path}", path);
                await _next(context);
                return;
            }

            // Check session for UserId
            var userId = context.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} is authenticated.", userId);
                await _next(context);
            }
            else
            {
                _logger.LogInformation("User not authenticated. Redirecting from path: {Path}", path);
                context.Response.Redirect("/Auth/Login");
            }
        }
    }
}
