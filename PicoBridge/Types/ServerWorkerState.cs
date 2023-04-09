using System.Net;
using System.Net.Sockets;

namespace PicoBridge.Types;

public struct ServerWorkerState
{
    public UdpClient Client;
    public IPEndPoint EndPoint;
}
