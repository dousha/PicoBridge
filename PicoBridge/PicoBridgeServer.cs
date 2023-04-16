using System.Linq;
using System.Net.NetworkInformation;
using PicoBridge.Exceptions;
using PicoBridge.Logic;
using PicoBridge.Types;

namespace PicoBridge;

public class PicoBridgeServer
{
    public delegate void ConnectivityChangeHandler(object sender, bool state);

    public delegate void DatagramHandler(object sender, PicoFaceTrackingDatagram datagram);

    private const int ServerPort = 29763;
    private PicoBridgeServerWorker? instance;

    public bool IsDeviceConnected { get; private set; }

    public void Start()
    {
        if (!IsUdpPortFree(ServerPort))
        {
            throw new NetworkException("Port already in use");
        }

        instance = new PicoBridgeServerWorker(ServerPort);
        instance.Connect += _ =>
        {
            IsDeviceConnected = true;
            ConnectivityChange?.Invoke(this, IsDeviceConnected);
        };
        instance.Disconnect += _ =>
        {
            IsDeviceConnected = false;
            ConnectivityChange?.Invoke(this, IsDeviceConnected);
        };
        instance.Data += (_, datagram) => { DatagramChange?.Invoke(this, datagram); };
        instance.Start();
    }

    public void Stop()
    {
        instance?.Stop();
    }

    public void Join()
    {
        instance?.Join();
    }

    private static bool IsUdpPortFree(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var endpoints = properties.GetActiveUdpListeners();
        return endpoints.All(it => it.Port != port);
    }

    public event ConnectivityChangeHandler? ConnectivityChange;

    public event DatagramHandler? DatagramChange;
}
