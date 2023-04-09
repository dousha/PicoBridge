using System.Net;
using System.Net.Sockets;
using PicoBridge.Types;

namespace PicoBridge.Logic;

public class PicoBridgeServerWorker
{
    internal PicoBridgeServerWorker(int port)
    {
        _cts = new CancellationTokenSource();
        _port = port;
        _receiveStream = new MemoryStream(_receiveBuffer);
    }

    public void Start()
    {
        ThreadPool.QueueUserWorkItem(Work, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    public delegate void ConnectEventHandler(object sender);

    public event ConnectEventHandler? Connect;

    public delegate void DisconnectEventHandler(object sender);

    public event DisconnectEventHandler? Disconnect;

    public delegate void DatagramEventHandler(object sender, PicoFaceTrackingDatagram datagram);

    public event DatagramEventHandler? Data;

    private void Work(object? obj)
    {
        var token = (CancellationToken)obj!;
        var listener = new UdpClient(_port);
        var endpoint = new IPEndPoint(IPAddress.Any, _port);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var data = listener.Receive(ref endpoint);
                _receiveStream.Write(data);
                while (_receiveStream.Length >= PicoDatagramSize)
                {
                    var datagram = new PicoFaceTrackingDatagram(_receiveStream);
                    Data?.Invoke(this, datagram);
                }
            }
        }
        finally
        {
            listener.Close();
        }
    }

    private readonly CancellationTokenSource _cts;
    private readonly int _port;
    private readonly MemoryStream _receiveStream;
    private readonly byte[] _receiveBuffer = new byte[2048];

    private const int PicoDatagramSize = 892;
}