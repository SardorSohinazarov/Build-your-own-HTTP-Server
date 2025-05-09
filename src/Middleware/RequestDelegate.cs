using codecrafters_http_server.src.Http;

namespace codecrafters_http_server.src.Middleware
{
    public delegate Task RequestDelegate(HttpContext context);
}
