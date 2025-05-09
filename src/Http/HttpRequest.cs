using codecrafters_http_server.src.Http;
using System.Net.Sockets;
using System.Text;
using HttpMethod = codecrafters_http_server.src.Http.HttpMethod;

public class HttpRequest
{
    public HttpRequest(Socket clientSocket)
    {
        ClientSocket = clientSocket; // bu kod kliyent soketini saqlaydi.
        Parse(clientSocket);
    }

    public HttpMethod Method { get; set; }
    public Host Host { get; set; }
    public string Path { get; set; }
    public string Body { get; set; }
    public Socket ClientSocket { get; set; }
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