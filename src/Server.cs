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
                string message = request.Path[6..]; // bu kod URLdan xabarni ajratib oladi.
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
                    string fileName = request.Path[7..]; // bu kod URLdan fayl nomini ajratib oladi.
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