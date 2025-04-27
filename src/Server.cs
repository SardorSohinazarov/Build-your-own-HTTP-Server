using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static void Main(string[] args)
    {
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

    private static void HandleClient(Socket clientSocket,string[] args)
    {
        Console.WriteLine("Kliyentga ulanish boshlandi");
        // bu kod kliyentdan keladigan ma'lumotlarni qabul qiladi va javob yuboradi.
        // Kliyentdan keladigan ma'lumotlarni qabul qilish uchun massiv yaratamiz.
        while (clientSocket.Connected)
        {
            ProcessRequest(clientSocket, args);
        }
    }

    private static void ProcessRequest(Socket clientSocket, string[] args)
    {
        try
        {
            var request = new HttpRequest(clientSocket); // bu kod kliyentdan keladigan so'rovni qabul qiladi va uni HttpRequest obyektiga aylantiradi.

            var response = new HttpResponse();

            if (request.Path == "/")
            {
                response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
            }
            else if (request.Path.StartsWith("/echo/"))
            {
                string message = request.Path.Substring(6, request.Path.Length - 6); // bu kod URLdan xabarni ajratib oladi.
                response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
                response.AddHeader("Content-Type", "text/plain"); // bu kod javobning sarlavhasini belgilaydi.
                response.AddHeader("Content-Length", message.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
                response.Body = message; // bu kod javobning tanasini belgilaydi.
            }
            else if (request.Path.StartsWith("/user-agent"))
            {
                response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
                string userAgent = request.Headers["User-Agent"]; // bu kod so'rovdan user-agentni ajratib oladi.
                response.AddHeader("Content-Type", "text/plain"); // bu kod javobning sarlavhasini belgilaydi.
                response.AddHeader("Content-Length", userAgent.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
                response.Body = userAgent; // bu kod javobning tanasini belgilaydi.
            }
            else if (request.Path.StartsWith("/files/"))
            {
                try
                {
                    string fileName = request.Path.Substring(7, request.Path.Length - 7); // bu kod URLdan fayl nomini ajratib oladi.
                    string fullPath = Path.Combine(args[1], fileName); // bu kod faylning to'liq yo'lini oladi.
                    //string fullPath = "/"; // bu kod faylning to'liq yo'lini oladi.
                    if (request.Method.ToString() == "POST")
                    {
                        using StreamWriter reader = new StreamWriter(fullPath);
                        reader.Write(request.Body);
                        response.StatusCode = 201; // bu kod javobning status kodini belgilaydi.
                    }
                    else
                    {
                        using StreamReader reader = new StreamReader(fullPath); // bu kod faylni o'qish uchun ochadi.
                        string fileContent = reader.ReadToEnd(); // bu kod faylning ichidagi ma'lumotlarni o'qiydi.
                        response.StatusCode = 200; // bu kod javobning status kodini belgilaydi.
                        response.AddHeader("Content-Type", "application/octet-stream"); // bu kod javobning sarlavhasini belgilaydi.
                        response.AddHeader("Content-Length", fileContent.Length.ToString()); // bu kod javobning sarlavhasini belgilaydi.
                        response.Body = fileContent; // bu kod javobning tanasini belgilaydi.
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    response.StatusCode = 404; // bu kod javobning status kodini belgilaydi.
                }
            }
            else
            {
                response.StatusCode = 404; // bu kod javobning status kodini belgilaydi.
            }

            if (request.Headers.ContainsKey("Connection"))
            {
                var connection = request.Headers["Connection"].Trim();
                if (connection == "close")
                {
                    response.AddHeader("Connection", "close");
                } 
            }

            byte[] responseBytes;

            if (request.Headers.ContainsKey("Accept-Encoding"))
            {
                var encodings = request.Headers["Accept-Encoding"].Split(",").Select(x => x.Trim()).ToList();
                if (encodings.Contains("gzip"))
                {
                    response.AddHeader("Content-Encoding", "gzip");
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            byte[] bodyBytes = Encoding.UTF8.GetBytes(response.Body);
                            gzipStream.Write(bodyBytes, 0, bodyBytes.Length);
                        }
                        var compressedBytes = compressedStream.ToArray();
                        response.Body = null;
                        response.Headers["Content-Length"] = compressedBytes.Length.ToString();
                        responseBytes = response.ToByteArray(); // bu kod javobni byte massiviga aylantiradi.
                        clientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.
                        clientSocket.Send(compressedBytes); // bu kod kliyentga siqilgan javobni yuboradi.
                        
                        if (request.Headers.ContainsKey("Connection"))
                        {
                            var connection = request.Headers["Connection"].Trim();
                            if (connection == "close")
                            {
                                clientSocket.Close(); // bu kod kliyentni yopadi.
                                return;
                            } 
                        }
                    }
                }
            }

            responseBytes = response.ToByteArray(); // bu kod javobni byte massiviga aylantiradi.

            clientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.

            if (request.Headers.ContainsKey("Connection"))
            {
                var connection = request.Headers["Connection"].Trim();
                if (connection == "close")
                {
                    clientSocket.Close(); // bu kod kliyentni yopadi.
                    return;
                } 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

public class Host
{
    public Host(string hostName, int port, string protocol)
    {
        HostName = hostName;
        Port = port;
        Protocol = protocol;
    }

    public string HostName { get; set; }
    public int Port { get; set; }
    public string Protocol { get; set; }
}

public class HttpRequest
{
    public HttpRequest(Socket clientSocket)
    {
        Parse(clientSocket);
    }

    public HttpMethod Method { get; set; }
    public Host Host { get; set; }
    public string Path { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }

    public void Parse(Socket socket)
    {
        byte[] buffer = new byte[4096]; // bu massiv kliyentdan keladigan ma'lumotlarni saqlaydi.
        var received = socket.Receive(buffer); // bu metod kliyentdan ma'lumotlarni qabul qiladi va buffer massiviga saqlaydi.

        string requestText = Encoding.UTF8.GetString(buffer, 0, received); // bu metod buffer massividagi ma'lumotlarni stringga aylantiradi.
        Console.WriteLine($"So'rov:\n{requestText}");
        this.Method = GetMethod(requestText);
        this.Host = GetHost(requestText);
        this.Path = GetPath(requestText);
        this.Body = GetBody(requestText);
        this.Headers = GetHeaders(requestText);
    }

    private Host GetHost(string requestText)
    {
        var splitted = requestText.Split("\r\n");
        var host = splitted[1].Split(": ")[1];
        var protocol = splitted[0].Split(" ")[2];
        var port = 80;
        if (host.Contains(":"))
        {
            port = int.Parse(host.Split(":")[1]);
            host = host.Split(":")[0];
        }

        return new Host(host, port, protocol); // bu kod Host obyektini yaratadi va uni qaytaradi.
    }

    private Dictionary<string, string> GetHeaders(string requestText)
    {
        string[] sections = requestText.Split("\r\n\r\n"); // bu kod so'rovning tanasini ajratib oladi.
        string[] lines = sections[0].Split("\r\n"); // bu kod so'rovning sarlavhalarini ajratib oladi.
        string[] headerLines = lines[2..]; // bu kod so'rovning sarlavhalarini ajratib oladi.
        Dictionary<string, string> headers = new Dictionary<string, string>(); // bu kod sarlavhalar uchun lug'at yaratadi.
        foreach (var line in headerLines)
        {
            var header = line.Trim().Split(": "); // bu kod sarlavhalarni ajratib oladi.
            if (header.Length == 2)
            {
                headers.Add(header[0], header[1]); // bu kod sarlavhalarni lug'atga qo'shadi.
            }
        }
        return headers; // bu kod sarlavhalarni qaytaradi.
    }

    private string GetBody(string requestText)
    {
        string[] sections = requestText.Split("\r\n\r\n");
        string body = sections.Length > 1 ? sections[1] : "";
        return body.Trim();
    }

    private string GetPath(string requestText)
    {
        var splitted = requestText.Split("\r\n");
        var route = splitted[0].Split(" ")[1];
        return route;
    }

    private HttpMethod GetMethod(string request)
    {
        var method = request.Split(" ")[0]; // bu kod so'rovning metodini ajratib oladi.
        return method switch
        {
            "GET" => HttpMethod.GET,
            "POST" => HttpMethod.POST,
            "PUT" => HttpMethod.PUT,
            "DELETE" => HttpMethod.DELETE,
            _ => throw new NotImplementedException()
        };
    }
}

public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE
}

public static class HttpStatusMessages
{
    public static string GetMessage(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            400 => "Bad Request",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Unknown Status"
        };
    }
}

public class HttpResponse
{
    public int StatusCode { get; set; }
    public string StatusMessage => HttpStatusMessages.GetMessage(StatusCode);
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; }

    public byte[] ToByteArray()
    {
        StringBuilder responseBuilder = new StringBuilder();

        responseBuilder.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
        if (Headers != null)
        {
            foreach (var header in Headers)
            {
                responseBuilder.Append($"{header.Key}: {header.Value}\r\n");
            }
        }
        responseBuilder.Append("\r\n");

        if (!string.IsNullOrEmpty(Body))
        {
            responseBuilder.Append(Body);
        }

        return Encoding.UTF8.GetBytes(responseBuilder.ToString());
    }

    public void AddHeader(string key, string value)
    {
        if (Headers == null)
        {
            Headers = new Dictionary<string, string>();
        }
        Headers.Add(key, value);
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