// See https://aka.ms/new-console-template for more information

using PicoBridge;

var server = new PicoBridgeServer();
server.ConnectivityChange += (_, state) => { Console.WriteLine(state ? "Device connected" : "Device disconnected"); };
server.DatagramChange += (_, datagram) => { Console.WriteLine($"{datagram.Timestamp} BSW[0]={datagram.BlendShapeWeight[0]}"); };
Console.WriteLine("Starting server");
server.Start();
Console.WriteLine("Server started, joining threads");
server.Join();
