using MediatR;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static void Main(string[] args)
    {
        // You can use print statements as follows for debugging, they'll be visible when running tests.
        Console.WriteLine("Logs from your program will appear here!");

        // Uncomment this block to pass the first stage
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start(); // serverni boshlaydi, ya'ni soket ochiladi va eshitishni boshlaydi.
        Console.WriteLine("4221 eshitish boshlandi!");

        while (true)
        {
            Socket clientSocket = server.AcceptSocket(); // bu metod kliyent ulanmaguncha kutadi. Kimdir ulanadi, shunda bu metod ulanishni qabul qiladi va Socket obyektini qaytaradi.
            Console.WriteLine("Client ulandi");
            Thread clientThread = new Thread(() => HandleClient(clientSocket,args)); // bu kod yangi ipni yaratadi va unga kliyentni uzatadi.
            clientThread.Start(); // bu kod yangi ipni ishga tushiradi.
        }

        server.Stop(); // serverni to'xtatadi, ya'ni soketni yopadi.

    }
    static void HandleClient(Socket clientSocket,string[] args)
    {
        Console.WriteLine("Kliyentga ulanish boshlandi");
        // bu kod kliyentdan keladigan ma'lumotlarni qabul qiladi va javob yuboradi.
        // Kliyentdan keladigan ma'lumotlarni qabul qilish uchun massiv yaratamiz.

        byte[] buffer = new byte[4096]; // bu massiv kliyentdan keladigan ma'lumotlarni saqlaydi.

        try
        {
            var received = clientSocket.Receive(buffer); // bu metod kliyentdan ma'lumotlarni qabul qiladi va buffer massiviga saqlaydi.

            string requestText = Encoding.UTF8.GetString(buffer, 0, received); // bu metod buffer massividagi ma'lumotlarni stringga aylantiradi.
            Console.WriteLine($"So'rov:\n{requestText}");

            var splitted = requestText.Split("\r\n");
            string[] sections = requestText.Split("\r\n\r\n"); // bu kod so'rovning tanasini ajratib oladi.
            string headers = sections[0]; // bu kod so'rovning sarlavhalarini ajratib oladi.
            string body = sections.Length > 1 ? sections[1] : ""; // bu kod so'rovning tanasini ajratib oladi.
            var url = splitted[1].Split(" ")[1]; // bu kod so'rovning URL qismini ajratib oladi.
            Console.WriteLine($"Url->{url}");

            var method = splitted[0].Split(" ")[0];

            var route = splitted[0].Split(" ")[1];

            byte[] responseBytes;
            string response = "";

            if (route == "/")
            {
                response = "HTTP/1.1 200 OK\r\n\r\n";
                responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
            }
            else if (route.StartsWith("/echo/"))
            {
                string message = route.Substring(6, route.Length - 6); // bu kod URLdan xabarni ajratib oladi.
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n" + message; // bu kod javobni tayyorlaydi.
                responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
            }
            else if (route.StartsWith("/user-agent"))
            {
                string userAgent = splitted[2].Split(": ")[1]; // bu kod so'rovdan user-agentni ajratib oladi.
                response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n" + userAgent; // bu kod javobni tayyorlaydi.
                responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
            }
            else if (route.StartsWith("/files/"))
            {
                try
                {
                    string fileName = route.Substring(7, route.Length - 7); // bu kod URLdan fayl nomini ajratib oladi.
                    string fullPath = Path.Combine(args[1],fileName); // bu kod faylning to'liq yo'lini oladi.
                    if(method == "POST")
                    {
                        using StreamWriter reader = new StreamWriter(fullPath);
                        reader.Write(body.Trim());
                        response = $"HTTP/1.1 201 Created\r\n\r\n";
                    }
                    else
                    {
                        using StreamReader reader = new StreamReader(fullPath); // bu kod faylni o'qish uchun ochadi.
                        string fileContent = reader.ReadToEnd(); // bu kod faylning ichidagi ma'lumotlarni o'qiydi.
                        response = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n{fileContent}"; // bu kod javobni tayyorlaydi.
                    }
                    responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    response = "HTTP/1.1 404 Not Found\r\n\r\n"; // bu kod javobni tayyorlaydi.
                    responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
                }
            }
            else
            {
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
                responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
            }

            clientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        clientSocket.Close(); // bu metod kliyentni yopadi.
    }
}




//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//namespace codecrafters_http_server.src;
//public class HttpServer {
//  private readonly TcpListener _listener;
//  private readonly Dictionary<string, IRequestHandler> _routes;
//  private bool _isRunning;
//  public HttpServer(IPAddress ipAddress, int port) {
//    _listener = new TcpListener(ipAddress, port);
//    _routes = new Dictionary<string, IRequestHandler>();
//  }
//  public void AddRoute(string path, IRequestHandler handler) {
//    _routes[path] = handler;
//  }
//  public void Start() {
//    _isRunning = true;
//    _listener.Start();
//    Console.WriteLine(
//        $"Server started and listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");
//    try {
//      while (_isRunning) {
//        // Wait for a client to connect
//        var client = _listener.AcceptSocket();
//        Task.Run(() => ProcessClientRequest(client));
//      }
//    } catch (Exception ex) {
//      Console.WriteLine($"Server error: {ex.Message}");
//    } finally {
//      Stop();
//    }
//  }
//  public void Stop() {
//    _isRunning = false;
//    _listener.Stop();
//    Console.WriteLine("Server stopped");
//  }
//  private void ProcessClientRequest(Socket client) {
//    try {
//      using (client) {
//        while (client.Connected) {
//          var request = HttpRequest.Parse(client);
//          if (request == null)
//            break;
//          var response = RouteRequest(request);
//          SendResponse(client, response);
//        }
//      }
//    } catch (Exception ex) {
//      Console.WriteLine($"Error processing request: {ex.Message}");
//    }
//  }
//  private HttpResponse RouteRequest(HttpRequest request) {
//    // Extract the base path from the request path
//    string path = request.Path.Split('/', 3)[1];
//    string basePath = "/" + path;
//    // Find the appropriate handler
//    if (_routes.TryGetValue(basePath, out var handler)) {
//      return handler.HandleRequest(request);
//    }
//    // Return 404 if no handler is found
//    return new HttpResponse { StatusCode = 404, StatusMessage = "Not Found" };
//  }
//  private void SendResponse(Socket client, HttpResponse response) {
//    byte[] responseBytes = response.ToByteArray();
//    client.Send(responseBytes, SocketFlags.None);
//  }
//}