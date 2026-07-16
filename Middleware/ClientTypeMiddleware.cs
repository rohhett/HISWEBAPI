namespace HISWEBAPI.Middleware
{
    public class ClientTypeMiddleware
    {
        private readonly RequestDelegate _next;
        public ClientTypeMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            string clientType = context.Request.Path.StartsWithSegments("/mob")
                ? "Mobile"
                : "Web";

            // Optional: let the app override/refine this (e.g. "Mobile-Android-v2.3")
            if (context.Request.Headers.TryGetValue("X-Client-Type", out var headerVal))
                clientType = headerVal.ToString();

            log4net.ThreadContext.Properties["ClientType"] = clientType;
            context.Items["ClientType"] = clientType;

            await _next(context);
        }
    }
}
