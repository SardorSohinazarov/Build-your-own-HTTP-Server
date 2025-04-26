using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start(); // serverni boshlaydi, ya'ni soket ochiladi va eshitishni boshlaydi.
Console.WriteLine("4221 eshitish boshlandi!");
var clientSocket = server.AcceptSocket(); // bu metod kliyent ulanmaguncha kutadi. Kimdir ulanadi, shunda bu metod ulanishni qabul qiladi va Socket obyektini qaytaradi.
Console.WriteLine("Client ulandi");

string response = "HTTP/1.1 200 OK\r\n\r\n";
byte[] responseBytes = Encoding.UTF8.GetBytes(response); // bu metod serverdan kliyentga HTTP javobini yuboradi.
clientSocket.Send(responseBytes); // bu metod kliyentga javob yuboradi.
clientSocket.Close(); // bu metod kliyentni yopadi.
server.Stop(); // serverni to'xtatadi, ya'ni soketni yopadi.