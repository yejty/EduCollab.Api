namespace EduCollab.Api.Middleware
{
    public sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        public const string RequestIdItemKey = "RequestId";
        public const string RequestIdHeaderName = "X-Request-Id";

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString("N");
            context.Items[RequestIdItemKey] = requestId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[RequestIdHeaderName] = requestId;
                return Task.CompletedTask;
            });

            using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
            {
                await next(context);
            }
        }

        public static string GetRequestId(HttpContext context) =>
            context.Items.TryGetValue(RequestIdItemKey, out var value) && value is string id
                ? id
                : string.Empty;
    }
}
