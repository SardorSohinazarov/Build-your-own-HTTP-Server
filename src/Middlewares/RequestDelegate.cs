using codecrafters_http_server.src.Http;

namespace codecrafters_http_server.src.Middlewares
{
    public delegate Task RequestDelegate(HttpContext context);
}
