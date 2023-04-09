using System.Net;
using System.Net.NetworkInformation;
using PicoBridge.Exceptions;
using PicoBridge.Logic;

namespace PicoBridge;

public class PicoBridgeServer
{
    public bool IsDeviceConnected()
    {
        return false;
    }

    public void Start()
    {
        if (!IsUdpPortFree(ServerPort))
        {
            throw new NetworkException("Port already in use");
        }
        
        Instance.Start();
    }

    public void Stop()
    {
        Instance.Stop();
    }

    private static bool IsUdpPortFree(int port)
    {
        IPEndPoint[] endPoints;
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var endpoints = properties.GetActiveUdpListeners();
        return endpoints.All(it => it.Port != port);
    }

    private const int ServerPort = 29763;
    private PicoBridgeServerWorker Instance { get; } = new PicoBridgeServerWorker(ServerPort);
}