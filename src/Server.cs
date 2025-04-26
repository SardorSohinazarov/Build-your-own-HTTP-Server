using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start(); // serverni boshlaydi, ya'ni soket ochiladi va eshitishni boshlaydi.
Console.WriteLine("4221 eshitish boshlandi!");
Socket clientSocket = server.AcceptSocket(); // bu metod kliyent ulanmaguncha kutadi. Kimdir ulanadi, shunda bu metod ulanishni qabul qiladi va Socket obyektini qaytaradi.
Console.WriteLine("Client ulandi");

byte[] buffer = new byte[4096]; // bu massiv kliyentdan keladigan ma'lumotlarni saqlaydi.
var received = clientSocket.Receive(buffer); // bu metod kliyentdan ma'lumotlarni qabul qiladi va buffer massiviga saqlaydi.

string requestText = Encoding.UTF8.GetString(buffer, 0, received); // bu metod buffer massividagi ma'lumotlarni stringga aylantiradi.
Console.WriteLine($"So'rov:\n{requestText}");

var splitted = requestText.Split("\r\n");
var url = splitted[1].Split(" ")[1]; // bu kod so'rovning URL qismini ajratib oladi.
Console.WriteLine($"Url->{url}");

var route = splitted[0].Split(" ")[1];

byte[] responseBytes;
if(route == "/")
{
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
}else if (route.StartsWith("/echo/"))
{
    string message = route.Substring(6,route.Length - 6); // bu kod URLdan xabarni ajratib oladi.
    string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n" + message; // bu kod javobni tayyorlaydi.
    responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
}
else
{
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
}

clientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.
clientSocket.Close(); // bu metod kliyentni yopadi.
server.Stop(); // serverni to'xtatadi, ya'ni soketni yopadi.