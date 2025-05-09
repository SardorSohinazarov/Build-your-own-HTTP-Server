using codecrafters_http_server.src.Http;

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
    }
}
