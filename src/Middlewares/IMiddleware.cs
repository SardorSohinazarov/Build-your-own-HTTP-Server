using codecrafters_http_server.src.Http;

namespace codecrafters_http_server.src.Middlewares
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context, RequestDelegate next);
    }
}
