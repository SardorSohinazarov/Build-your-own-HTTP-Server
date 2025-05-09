using codecrafters_http_server.src.Http;
using codecrafters_http_server.src.Middleware;
using System.Net;
using System.Net.Sockets;

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
            middlewareBuilder.MapGet("/qales", async context =>
            {
                Console.WriteLine("Zo'r");
            });
            var app = middlewareBuilder.Run(httpContext);
            await app(httpContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}