using System;
using Fleck;
using FoolCardServer;

string host = args.Length > 0 ? args[0] : "0.0.0.0";
int port = args.Length > 1 ? int.Parse(args[1]) : 61018;

var manager = new RoomManager();
var server  = new WebSocketServer($"ws://{host}:{port}");

server.Start(socket =>
{
    socket.OnOpen    = () => manager.OnOpen(socket);
    socket.OnClose   = () => manager.OnClose(socket);
    socket.OnMessage = msg => manager.OnMessage(socket, msg);
    socket.OnError   = ex => Console.WriteLine($"[ERR] {ex.Message}");
});

Console.WriteLine($"FoolCard Server listening on ws://{host}:{port}");
Console.WriteLine("Press Enter to quit.");
Console.ReadLine();
