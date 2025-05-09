using codecrafters_http_server.src.Http;
using codecrafters_http_server.src.Middlewares;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start(); // serverni boshlaydi, ya'ni soket ochiladi va eshitishni boshlaydi.
        Console.WriteLine("4221 eshitish boshlandi!");

        while (true)
        {
            Socket clientSocket = server.AcceptSocket(); // bu metod kliyent ulanmaguncha kutadi. Kimdir ulanadi, shunda bu metod ulanishni qabul qiladi va Socket obyektini qaytaradi.
            Console.WriteLine("Client ulandi");
            Thread clientThread = new Thread(async () => await HandleClient(clientSocket,args)); // bu kod yangi threadteni yaratadi va unga kliyentni uzatadi.
            clientThread.Start(); // yangi threadda so'rovni handle qilishni boshlaydi.
        }

        server.Stop(); // serverni to'xtatadi, ya'ni soketni yopadi.
    }

    private static async Task HandleClient(Socket clientSocket,string[] args)
    {
        Console.WriteLine("Kliyentga ulanish boshlandi");
        // bu kod kliyentdan keladigan ma'lumotlarni qabul qiladi va javob yuboradi.
        if (clientSocket.Connected)
            await ProcessRequest(clientSocket, args);
    }

    private static async Task ProcessRequest(Socket clientSocket, string[] args)
    {
        try
        {
            var request = new HttpRequest(clientSocket); // bu kod kliyentdan keladigan so'rovni qabul qiladi va uni HttpRequest obyektiga aylantiradi.
            var response = new HttpResponse();

            var httpContext = new HttpContext(request, response); // bu kod so'rov va javobni birlashtiradi.

            var middlewareBuilder = new MiddlewareBuilder();
            middlewareBuilder.MapGet("/qales", QalesMiddleware());

            var app = middlewareBuilder.Build((HttpContext) => FinalHandler(httpContext));
            await app(httpContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static Func<HttpContext, Task> QalesMiddleware()
    {
        return async (httpContext) =>
        {
            Console.WriteLine("\nYaxshi\n");
            Console.WriteLine("\nYaxshi\n");
            Console.WriteLine("\nYaxshi\n");
            Console.WriteLine("\nYaxshi\n");
        };
    }

    private static async Task FinalHandler(HttpContext httpContext)
    {
        byte[] responseBytes;

        #region Handlers
        if (httpContext.Request.Path == "/")
        {
            httpContext.Response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
        }
        else if (httpContext.Request.Path.StartsWith("/echo/"))
        {
            string message = httpContext.Request.Path[6..]; // bu kod URLdan xabarni ajratib oladi.
            httpContext.Response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
            httpContext.Response.AddHeader("Content-Type", "text/plain"); // bu kod javobning sarlavhasini belgilaydi.
            httpContext.Response.AddHeader("Content-Length", message.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
            httpContext.Response.Body = message; // bu kod javobning tanasini belgilaydi.
        }
        else if (httpContext.Request.Path.StartsWith("/user-agent"))
        {
            httpContext.Response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
            string userAgent = httpContext.Request.Headers["User-Agent"]; // bu kod so'rovdan user-agentni ajratib oladi.
            httpContext.Response.AddHeader("Content-Type", "text/plain"); // bu kod javobning sarlavhasini belgilaydi.
            httpContext.Response.AddHeader("Content-Length", userAgent.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
            httpContext.Response.Body = userAgent; // bu kod javobning tanasini belgilaydi.
        }
        else if (httpContext.Request.Path.StartsWith("/files/"))
        {
            try
            {
                string fileName = httpContext.Request.Path[7..]; // bu kod URLdan fayl nomini ajratib oladi.
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName); // bu kod faylning to'liq yo'lini oladi.
                                                                   //string fullPath = "/"; // bu kod faylning to'liq yo'lini oladi.
                if (httpContext.Request.Method.ToString() == "POST")
                {
                    using StreamWriter reader = new StreamWriter(fullPath);
                    reader.Write(httpContext.Request.Body);
                    httpContext.Response.StatusCode = 201; // bu kod javobning status kodini belgilaydi.
                }
                else
                {
                    using StreamReader reader = new StreamReader(fullPath); // bu kod faylni o'qish uchun ochadi.
                    string fileContent = reader.ReadToEnd(); // bu kod faylning ichidagi ma'lumotlarni o'qiydi.
                    httpContext.Response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
                    httpContext.Response.AddHeader("Content-Type", "application/octet-stream"); // bu kod javobning sarlavhasini belgilaydi.
                    httpContext.Response.AddHeader("Content-Length", fileContent.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
                    httpContext.Response.Body = fileContent; // bu kod javobning tanasini belgilaydi.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                httpContext.Response.StatusCode = 404; // bu kod javobning status kodini belgilaydi.
            }
        }
        else
        {
            httpContext.Response.StatusCode = 404; // bu kod javobning status kodini belgilaydi.
        }
        if (httpContext.Request.Headers.ContainsKey("Connection"))
        {
            var connection = httpContext.Request.Headers["Connection"].Trim();
            if (connection == "close")
            {
                httpContext.Response.AddHeader("Connection", "close");
            }
        }
        if (httpContext.Request.Headers.ContainsKey("Accept-Encoding"))
        {
            var encodings = httpContext.Request.Headers["Accept-Encoding"].Split(",").Select(x => x.Trim()).ToList();
            if (encodings.Contains("gzip"))
            {
                httpContext.Response.AddHeader("Content-Encoding", "gzip");
                using (var compressedStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    {
                        byte[] bodyBytes = Encoding.UTF8.GetBytes(httpContext.Response.Body);
                        gzipStream.Write(bodyBytes, 0, bodyBytes.Length);
                    }
                    var compressedBytes = compressedStream.ToArray();
                    httpContext.Response.Body = null;
                    httpContext.Response.Headers["Content-Length"] = compressedBytes.Length.ToString();
                    responseBytes = httpContext.Response.ToByteArray(); // bu kod javobni byte massiviga aylantiradi.
                    httpContext.Request.ClientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.
                    httpContext.Request.ClientSocket.Send(compressedBytes); // bu kod kliyentga siqilgan javobni yuboradi.

                    if (httpContext.Request.Headers.ContainsKey("Connection"))
                    {
                        var connection = httpContext.Request.Headers["Connection"].Trim();
                        if (connection == "close")
                        {
                            httpContext.Request.ClientSocket.Close(); // bu kod kliyentni yopadi.
                            return;
                        }
                    }
                }
            }
        }
        #endregion

        responseBytes = httpContext.Response.ToByteArray(); // bu kod javobni byte massiviga aylantiradi.
        httpContext.Request.ClientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.

        if (httpContext.Request.Headers.ContainsKey("Connection"))
        {
            var connection = httpContext.Request.Headers["Connection"].Trim();
            if (connection == "close")
            {
                httpContext.Request.ClientSocket.Close(); // bu kod kliyentni yopadi.
                return;
            }
        }
    }
}