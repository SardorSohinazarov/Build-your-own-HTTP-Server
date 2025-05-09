using codecrafters_http_server.src.Http;
using HttpMethod = codecrafters_http_server.src.Http.HttpMethod;

namespace codecrafters_http_server.src.Middlewares
{
    public class MiddlewareBuilder
    {
        private readonly List<Func<HttpContext, Func<Task>,Task>> _middlewares = new();

        public MiddlewareBuilder Use(Func<HttpContext, Func<Task>, Task> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        public Func<HttpContext, Task> Build(Func<HttpContext, Task> finalHandler)
        {
            Func<HttpContext, Task> app = finalHandler;

            foreach (var middleware in _middlewares.AsEnumerable().Reverse())
            {
                var next = app;  // Oldingi handlerni saqlab qo'yamiz
                app = (context) => middleware(context, () => next(context));  // Keyingi handlerni chaqirish
            }

            return app;
        }

        public void MapGet(string path, Func<HttpContext, Task> handler)
        {
            Use(async (context, next) =>
            {
                if (context.Request.Path == path && context.Request.Method == HttpMethod.GET)
                {
                    await handler(context);
                }
                else
                {
                    await next();
                }
            });
        }

        public void MapPost(string path, Func<HttpContext, Task> handler)
        {
            Use(async (context, next) =>
            {
                if (context.Request.Path == path && context.Request.Method == HttpMethod.POST)
                {
                    await handler(context);
                }
                else
                {
                    await next();
                }
            });
        }
    }
}
